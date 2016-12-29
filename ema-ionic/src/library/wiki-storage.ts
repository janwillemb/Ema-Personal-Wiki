import { LoggingService } from './logging-service';
import { StoredFile } from './stored-file';
import { Injectable } from '@angular/core';
import { File, WriteOptions, Entry } from 'ionic-native';
import { Storage } from '@ionic/storage';
declare var cordova: any;
declare function require(name: string);

@Injectable()
export class WikiStorage {

    private useSdCard: boolean;
    private storageDir: string;
    private readonly personalWikiKey: string = "PersonalWiki";
    private readonly personalWikiPrefix: string = this.personalWikiKey + ".";
    private checksum = require("checksum");

    constructor(
        private loggingService: LoggingService,
        private storage: Storage) {

        this.useSdCard = cordova && cordova.file;
        if (this.useSdCard) {
            this.storageDir = cordova.file.externalRootDirectory;
        }
    }


    private getPersonalWikiDir(): string {
        return this.storageDir + this.personalWikiKey;
    }

    listFiles(): Promise<string[]> {
        //prepar dir
        return this.initialize()
            .then(() => {
                if (this.useSdCard) {
                    //list files in the sd card dir
                    return File.listDir(this.storageDir, this.personalWikiKey)
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

    

    /*
        Get a file from the wiki storage
    */
    getFileContents(fileName: string): Promise<StoredFile> {
        var promise: Promise<any>;
        if (this.useSdCard) {
            //read the file from sd card (that is, start reading and harvest the promise)
            promise = File.readAsText(this.getPersonalWikiDir(), fileName);
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
    save(fileName: string, contents: string): Promise<any> {
        if (this.useSdCard) {
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
        if (this.useSdCard) {
            return File.checkFile(this.getPersonalWikiDir(), fileName)
                .catch(err => false)
                .then((exists: boolean) => {
                    var result: Promise<any>;
                    if (exists) {
                        result = File.removeFile(this.storageDir, fileName);
                    } else {
                        result = Promise.resolve();
                    }
                    return result;
                });
        } else {
            return this.storage.remove(this.personalWikiPrefix + fileName);
        }
    }

    /**
     * make sure the directory exists
     */
    private initialize(): Promise<any> {
        if (this.useSdCard) {
            return File.checkDir(this.storageDir, this.personalWikiKey)
                .then(exists => {
                    if (!exists) {
                        throw "doesn't exist";
                    }
                })
                .catch(err => File.createDir(this.storageDir, this.personalWikiKey, false));
        } else {
            return Promise.resolve();
        }
    }
}