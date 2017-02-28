import { Settings } from './settings';
import { LoggingService } from './logging-service';
import { StoredFile } from './stored-file';
import { Injectable } from '@angular/core';
import { File, WriteOptions, Entry } from 'ionic-native';
import { Storage } from '@ionic/storage';
declare var cordova: any;
declare function require(name: string);

@Injectable()
export class WikiStorage {

    private readonly personalWikiPrefix: string = "PersonalWiki.";
    private checksum = require("checksum");

    constructor(
        private loggingService: LoggingService,
        private settings: Settings,
        private storage: Storage) {

    }

    private static get useSdCard(): boolean {
        return cordova && cordova.file;
    }

    static get storageDir(): string {
        if (WikiStorage.useSdCard) {
            return cordova.file.externalRootDirectory;
        }
        return "";
    }

    private getPersonalWikiDir(): string {
        return WikiStorage.storageDir + this.settings.getLocalWikiDirectory();
    }

    listFiles(): Promise<string[]> {
        //prepar dir
        return this.initialize()
            .then(() => {
                if (WikiStorage.useSdCard) {
                    //list files in the sd card dir
                    return File.listDir(WikiStorage.storageDir, this.settings.getLocalWikiDirectory())
                        .then((entries: Entry[]) => entries
                            //filter: only files
                            .filter(x => x.isFile)
                            .map(x => x.name));
                } else {
                    //list storage keys
                    return this.storage.keys()
                        .then(keys => keys
                            //filter: only files that start with our magical key
                            .filter(x => x.startsWith(this.personalWikiPrefix))
                            //remove our magical key
                            .map(x => x.slice(this.personalWikiPrefix.length)));
                }
            });
    }

    // wait until storage is available
    checkStorage(): Promise<any> {
        return new Promise<any>((resolve, reject) => {
            if (!WikiStorage.useSdCard) {
                resolve();
                return;
            }
            const testFileName = ".testWriteAccess";
            return File.writeFile(this.getPersonalWikiDir(), testFileName, ".", { replace: true })
                .then(() => File.readAsText(this.getPersonalWikiDir(), testFileName))
                .then(() => {
                    resolve();
                    File.removeFile(this.getPersonalWikiDir(), testFileName)
                        .catch(() => { });
                })
                //retry after 1 second
                .catch(() => setTimeout(() => this.checkStorage().then(() => resolve()), 1000));
        });
    }

    /*
        Get a file from the wiki storage
    */
    getTextFileContents(fileName: string): Promise<StoredFile> {
        var promise: Promise<any>;
        if (WikiStorage.useSdCard) {
            //read the file from sd card (that is, start reading and harvest the promise)
            promise = new Promise((resolve, reject) => {
                var readAction = () =>
                    File.readAsText(this.getPersonalWikiDir(), fileName)
                        .then(contents => resolve(contents));

                readAction()
                    //retry after 500 ms on failure
                    .catch(err => setTimeout(() => readAction().catch(err => reject(err)), 500));
            });
        } else {
            //read the file from sqlite storage
            promise = this.storage.get(this.personalWikiPrefix + fileName);
        }

        return promise.then(contents => {
            var s = new StoredFile(fileName, contents);
            if (contents && typeof (contents) === "string") {
                s.checksum = this.checksum(contents);
            }
            return s;
        }).catch(err => {
            throw { msg: "Error in getFileContents for " + fileName, error: err };
        });
    }

    /**
     * save a file to the wiki storage
     */
    save(fileName: string, contents: any): Promise<any> {
        if (WikiStorage.useSdCard) {
            return File.writeFile(this.getPersonalWikiDir(),
                fileName,
                contents,
                <WriteOptions>{
                    replace: true
                });
        } else {
            return this.storage.set(this.personalWikiPrefix + fileName, contents);
        }
    }

    delete(fileName: string): Promise<any> {
        if (WikiStorage.useSdCard) {
            return File.removeFile(this.getPersonalWikiDir(), fileName);
        } else {
            return this.storage.remove(this.personalWikiPrefix + fileName);
        }
    }

    move(oldDir: string, newDir: string) {
        if (WikiStorage.useSdCard) {
            return File.moveDir(WikiStorage.storageDir, oldDir, WikiStorage.storageDir, newDir);
        }
        return Promise.resolve();
    }

    /**
     * make sure the directory exists
     */
    private initialize(): Promise<any> {
        if (WikiStorage.useSdCard) {
            return File.checkDir(WikiStorage.storageDir, this.settings.getLocalWikiDirectory())
                .then(exists => {
                    if (!exists) {
                        throw "doesn't exist";
                    }
                })
                .catch(err => File.createDir(WikiStorage.storageDir, this.settings.getLocalWikiDirectory(), false));
        } else {
            return Promise.resolve();
        }
    }
}