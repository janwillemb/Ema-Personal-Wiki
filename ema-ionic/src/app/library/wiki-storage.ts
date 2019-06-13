import { Settings } from './settings';
import { LoggingService } from './logging-service';
import { StoredFile } from './stored-file';
import { Injectable } from '@angular/core';
import { File, Entry } from '@ionic-native/file/ngx';
import { Storage } from '@ionic/storage';
import * as hash from 'object-hash';
import { AndroidPermissions } from '@ionic-native/android-permissions/ngx';

declare var cordova: any;

@Injectable()
export class WikiStorage {

    private readonly personalWikiPrefix: string = 'PersonalWiki.';

    constructor(
        private file: File,
        private androidPermissions: AndroidPermissions,
        private loggingService: LoggingService,
        private settings: Settings,
        private storage: Storage) {
    }

    private static get useSdCard(): boolean {
        try {
            return cordova && cordova.file;
        } catch (error) {
            return false;
        }
    }

    static get storageDir(): string {
        if (WikiStorage.useSdCard) {
            return cordova.file.externalRootDirectory;
        }
        return '';
    }

    private getPersonalWikiDir(): string {
        return WikiStorage.storageDir + this.settings.getLocalWikiDirectory();
    }

    async listFiles(): Promise<string[]> {
        // prepar dir
        await this.initialize();

        if (WikiStorage.useSdCard) {
            // list files in the sd card dir
            const entries = await this.file.listDir(WikiStorage.storageDir, this.settings.getLocalWikiDirectory());
            // filter: only files
            return entries
                .filter(x => x.isFile)
                .map(x => x.name);
        } else {
            // list storage keys
            const keys = await this.storage.keys();
            return keys
                // filter: only files that start with our magical key
                .filter(x => x.startsWith(this.personalWikiPrefix))
                // remove our magical key
                .map(x => x.slice(this.personalWikiPrefix.length));
        }
    }

    // wait until storage is available
    async checkStorage(): Promise<boolean> {
        if (!WikiStorage.useSdCard) {
            return Promise.resolve(true);
        }

        const storagePermission = this.androidPermissions.PERMISSION.WRITE_EXTERNAL_STORAGE;

        let hasPermission = false;
        try {
            const checkResult = await this.androidPermissions.checkPermission(storagePermission);
            if (checkResult.hasPermission) {
                hasPermission = true;
            }
        } catch (error) {
            this.loggingService.log('error checking storage permission', error);
        }

        if (hasPermission) {
            return Promise.resolve(true);
        }

        try {
            const reqResult = await this.androidPermissions.requestPermission(storagePermission);
            if (reqResult.hasPermission) {
                return Promise.resolve(true);
            }
        } catch (error) {
            this.loggingService.log('error asking storage permission', error);
        }
        return Promise.resolve(false);
    }

    /*
        Get a file from the wiki storage
    */
    getFileContents(fileName: string): Promise<StoredFile> {
        let promise: Promise<string | ArrayBuffer>;
        if (WikiStorage.useSdCard) {
            // read the file from sd card (that is, start reading and harvest the promise)
            promise = new Promise((resolve, reject) => {
                const readAction = () => {
                    if (fileName.endsWith('.txt')) {
                        return this.file.readAsText(this.getPersonalWikiDir(), fileName)
                            .then(contents => resolve(contents));
                    } else {
                        return this.file.readAsArrayBuffer(this.getPersonalWikiDir(), fileName)
                            .then(contents => resolve(contents));
                    }
                };

                // retry after 500 ms on failure
                readAction().catch(() => setTimeout(() => readAction().catch((err) => reject(err)), 500));
            });
        } else {
            // read the file from sqlite storage
            promise = this.storage.get(this.personalWikiPrefix + fileName);
        }

        return promise.then(contents => {
            const s = new StoredFile(fileName, contents);
            if (contents) {
                if (typeof contents === 'string') {
                    s.checksum = hash(contents);
                } else {
                    // don't calc checksum over binary files which tend to be very large
                    // just use the file size.
                    s.checksum = contents.byteLength.toString();
                }
            }
            return s;
        }).catch(err => {
            throw { msg: 'Error in getFileContents for ' + fileName, error: err };
        });
    }

    /**
     * save a file to the wiki storage
     */
    save(fileName: string, contents: any): Promise<any> {
        if (WikiStorage.useSdCard) {
            return this.file.writeFile(this.getPersonalWikiDir(),
                fileName,
                contents,
                { replace: true });
        } else {
            return this.storage.set(this.personalWikiPrefix + fileName, contents);
        }
    }

    delete(fileName: string): Promise<any> {
        if (WikiStorage.useSdCard) {
            return this.file.removeFile(this.getPersonalWikiDir(), fileName);
        } else {
            return this.storage.remove(this.personalWikiPrefix + fileName);
        }
    }

    move(oldDir: string, newDir: string): Promise<any> {
        if (WikiStorage.useSdCard) {
            return this.file.moveDir(WikiStorage.storageDir, oldDir, WikiStorage.storageDir, newDir);
        }
        return Promise.resolve();
    }

    /**
     * make sure the directory exists
     */
    private initialize(): Promise<any> {
        if (WikiStorage.useSdCard) {
            return this.file.checkDir(WikiStorage.storageDir, this.settings.getLocalWikiDirectory())
                .then(exists => {
                    if (!exists) {
                        throw new Error('doesn\'t exist');
                    }
                })
                .catch(() => this.file.createDir(WikiStorage.storageDir, this.settings.getLocalWikiDirectory(), false));
        } else {
            return Promise.resolve();
        }
    }
}