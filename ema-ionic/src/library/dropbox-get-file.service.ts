import { LoggingService } from './logging-service';
import { StoredFile } from './stored-file';
import { IDropboxEntry } from './idropbox-entry';
import { Settings } from './settings';
import { Injectable } from '@angular/core';
import { IDropboxAuth } from './idropbox-auth';
import { Http } from '@angular/http';
import { DropboxBase } from './dropbox-base';

@Injectable()
export class DropboxFileService extends DropboxBase {

    constructor(http: Http, settings: Settings, loggingService: LoggingService) {
        super(http, settings, loggingService);
    }

    download(entry: IDropboxEntry, auth: IDropboxAuth, byRevision?: boolean): Promise<StoredFile> {
        return new Promise<StoredFile>((resolve, reject) => {
            const isText = entry.name.endsWith(".txt");
            this.downloadFile(byRevision ? "rev:" + entry.rev : entry.id, isText, auth).subscribe(
                (value: ArrayBuffer | string) => resolve(new StoredFile(entry.name, value)),
                (error: any) => reject(error)
            );
        });
    }

    delete(entry: IDropboxEntry, auth: IDropboxAuth): Promise<any> {
        return new Promise<any>((resolve, reject) => {
            this.doRequest("https://api.dropboxapi.com/2/files/delete", auth, {
                path: this.settings.getRemotePath(entry.name)
            }).subscribe(
                () => resolve(),
                err => reject(err)
                );
        });
    }

    upload(file: StoredFile, auth: IDropboxAuth): Promise<any> {
        var fileName = file.fileName;
        return new Promise<any>((resolve, reject) => {
            this.uploadText(this.settings.getRemotePath(fileName), file.contents.toString(), auth).subscribe(
                () => resolve(),
                err => reject(err)
            );
        });
    }
}