import { Injectable } from '@angular/core';
import { WikiStorage } from './wiki-storage';
import { LoggingService } from './logging-service';
import { DropboxFileService } from './dropbox-get-file.service';
import { IDropboxEntry } from './idropbox-entry';
import { IDropboxAuth } from './idropbox-auth';
import { DropboxListFilesService } from './dropbox-list-files.service';
import { SyncState } from './sync-state';
import { StoredFile } from './stored-file';
import { Storage } from '@ionic/storage';
declare function require(name: string);

@Injectable()
export class DropboxSyncService {

    private readonly syncInfoStorageKey: string = ".wiki-v3-sync-info";
    private ignoreCase = require("ignore-case");
    private mergeText = require("plus.merge-text");
    private throat = require("throat")(1); //max 1 concurrent request to dropbox.

    constructor(
        private dropboxList: DropboxListFilesService,
        private dropboxFile: DropboxFileService,
        private loggingService: LoggingService,
        private wikiStorage: WikiStorage,
        private storage: Storage) {
    }

    syncFiles(auth: IDropboxAuth): SyncState {
        var state = new SyncState();
        state.makingProgress("Initializing");

        //make sure the directory exists remotely
        state.promise = this.dropboxList.initialize(auth)
            //get the previous list of files from the local directory
            .then(() => this.getLocalSyncInfo())
            //get list of files from dropbox
            .then((localList: IDropboxEntry[]) => {
                state.localSyncInfo = localList;
                state.makingProgress("Compare Dropbox with local...");
                return this.dropboxList.listFiles(auth, true);
            })
            .then((allRemoteFiles: IDropboxEntry[]) => {
                state.allRemoteFiles = allRemoteFiles;
                state.remoteChangedFiles = this.dropboxList.getChangedFiles(state.allRemoteFiles, state.localSyncInfo);
            })
            //get contents of all local files to compare with known checksum
            .then(() => this.wikiStorage.listFiles())
            .then((allLocalFiles: string[]) => {
                var promises = allLocalFiles.map(x => this.wikiStorage.getFileContents(x));
                return Promise.all(promises);
            })
            .then((allLocalFiles: StoredFile[]) => {
                state.allLocalFiles = allLocalFiles;
                state.localChangedFiles = state.allLocalFiles.filter(x => {
                    var fileInfoOnPreviousSync = state.localSyncInfo.find(y => this.ignoreCase.equals(y.name, x.fileName));
                    return !fileInfoOnPreviousSync || fileInfoOnPreviousSync.checksum !== x.checksum;
                });
                state.localDeletedFiles = state.localSyncInfo.filter(x => !allLocalFiles.find(y => this.ignoreCase.equals(x.name, y.fileName)));
            })
            //finally, process the gathered state 
            .then(() => {
                state.makingProgress("Apply changes...");

                //files, not changed locally, but changed or deleted remotely, have to been downloaded/deleted
                var filesToDownload = state.remoteChangedFiles.filter(x => {
                    var existsLocally = state.allLocalFiles.find(y => this.ignoreCase.equals(x.name, y.fileName));
                    return x.deleted && existsLocally || !x.deleted && !existsLocally;
                });

                //files changed locally, but unchanged remotely, can be uploaded safely
                var filesToUpload = state.localChangedFiles.filter(x => {
                    return !state.remoteChangedFiles.find(y => this.ignoreCase.equals(y.name, x.fileName) && !y.deleted);
                });
                //files deleted locally, but unchanged remotely, can be deleted safely
                var filesToUploadDeletion = state.localDeletedFiles.filter(x => {
                    return !state.remoteChangedFiles.find(y => this.ignoreCase.equals(y.name, x.name));
                });
                //(files, deleted locally and changed remotely, will be re-downloaded to the local storage. So be it.)
                //files, changed on both sides, have to be merged
                var filesToMerge = state.remoteChangedFiles.filter(x => {
                    if (x.deleted) {
                        return false; //just re-upload it in case of deletion (deletions are already excluded in the "filesToUpload" check)
                    }
                    return state.localChangedFiles.find(y => this.ignoreCase.equals(x.name, y.fileName));
                });

                //create the promises
                var downloadPromises: Promise<any>[] = filesToDownload.filter(x => !x.deleted && x.isFile).map(file =>
                    //map the list to a list of Promises that download the file: download it and save to storage
                    this.dropboxFile.download(file, auth)
                        .then((storedFile: StoredFile) => this.wikiStorage.save(storedFile.fileName, storedFile.contents))
                        .then(() => state.makingProgress())
                        .catch(err => {
                            this.loggingService.log("download failed for file " + file.name, err);
                            state.failedFiles.push(file.name);
                        }));

                //and do delete the remote deleted files
                var deletePromises: Promise<any>[] = filesToDownload.filter(x => x.deleted).map(file =>
                    this.wikiStorage.delete(file.name)
                        .then(() => state.makingProgress())
                        .catch(err => {
                            this.loggingService.log("delete failed for file " + file.name, err);
                            state.failedFiles.push(file.name);
                        }));

                //upload deletions to dropbox
                var deleteRemotePromises: Promise<any>[] = filesToUploadDeletion.map(file =>
                    this.dropboxFile.delete(file, auth)
                        .then(() => state.makingProgress())
                        .catch(err => {
                            this.loggingService.log("remote delete failed for file " + file.name, err);
                            state.failedFiles.push(file.name);
                        }));

                //download + merge + upload files changed on both sides
                var mergePromises: Promise<any>[] = filesToMerge.map(file => this.mergeFile(file, auth, state));

                var uploadPromises: Promise<any>[] = filesToUpload.map(file => {
                    return this.dropboxFile.upload(file, auth)
                        .then(() => state.makingProgress())
                        .catch(err => {
                            this.loggingService.log("upload failed for file " + file.fileName, err);
                            state.failedFiles.push(file.fileName);
                        });
                });

                this.loggingService.log("downloads: " + downloadPromises.length
                    + "; delete local: " + deletePromises.length
                    + "; delete remote: " + deleteRemotePromises.length
                    + "; merges: " + mergePromises.length
                    + "; uploads: " + uploadPromises.length);

                //wait until all promises have been fulfilled
                var allPromises: Promise<any>[] = []
                    .concat(downloadPromises)
                    .concat(deletePromises)
                    .concat(deleteRemotePromises)
                    .concat(mergePromises)
                    .concat(uploadPromises);

                state.setTotalSteps(allPromises.length);

                //throttle the requests: max 1 concurrent request to dropbox.
                allPromises = allPromises.map(x => this.throat(() => x));

                return Promise.all(allPromises);
            })
            //redownload current state from dropbox (after the uploads/deletions the last state has become stale)
            .then(() => {
                state.setTotalSteps(0);
                state.makingProgress("Save sync info...");
                return this.dropboxList.listFiles(auth);
            })
            .then((allRemoteFiles: IDropboxEntry[]) => {
                //remove failed files, it makes no sense to keep any state on that file
                var allSucceeded = allRemoteFiles.filter(x => state.failedFiles.indexOf(x.name) === -1);
                //calculate all checksums (= open all files to get contents to calc the checksum)
                var checksumPromises = allSucceeded.filter(x => x.isFile).map(x =>
                    this.wikiStorage.getFileContents(x.name).then(file => {
                        x.checksum = file.checksum;
                        return x;
                    }));
                return Promise.all(checksumPromises);
            })
            .then((newSyncState: IDropboxEntry[]) => {
                state.makingProgress("Sync finished");
                return this.saveLocalSyncInfo(newSyncState);
            });

        return state;
    }

    private mergeFile(remote: IDropboxEntry, auth: IDropboxAuth, state: SyncState): Promise<any> {

        var lastKnownLocalState = state.localSyncInfo.find(x => x.id === remote.id);

        var downloadCurrent = this.dropboxFile.download(remote, auth);
        var downloadParentRevision: Promise<StoredFile>;
        if (lastKnownLocalState) {
            downloadParentRevision = this.dropboxFile.download(lastKnownLocalState, auth, true);
        } else {
            //apparently both files are new, so make a random decision about which one to take
            downloadParentRevision = downloadCurrent;
        }
        var localFile = state.allLocalFiles.find(x => this.ignoreCase.equals(x.fileName, remote.name));

        return Promise.all([downloadCurrent, downloadParentRevision])
            .then((resolved: StoredFile[]) => {
                var latestRemoteState = resolved[0];
                var parentStateOfLocalChange = resolved[1];

                let origin = parentStateOfLocalChange.contents;
                let update1 = latestRemoteState.contents;
                let update2 = localFile.contents;
                let merged = this.mergeText.merge(origin, update1, update2);

                return this.wikiStorage.save(localFile.fileName, merged.toMarkdown());
            })
            //merge done, now upload result to dropbox
            .then(() => this.wikiStorage.getFileContents(localFile.fileName))
            .then((newLocalFile: StoredFile) => this.dropboxFile.upload(newLocalFile, auth))
            .then(() => state.makingProgress())
            .catch(err => {
                this.loggingService.log("merge failed for file " + remote.name, err);
                state.failedFiles.push(remote.name);
            });
    }

    /**
     * get local file with sync info state from the last sync
     */
    private getLocalSyncInfo(): Promise<IDropboxEntry[]> {
        return this.storage.get(this.syncInfoStorageKey)
            .then(value => <IDropboxEntry[]>(value || []));
    }

    private saveLocalSyncInfo(value: IDropboxEntry[]): Promise<any> {
        return this.storage.set(this.syncInfoStorageKey, value);
    }
}