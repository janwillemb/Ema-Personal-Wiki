import { LoggingService } from './logging-service';
import { Settings } from './settings';
import { DropboxBase } from './dropbox-base';
import { IDropboxEntry } from './idropbox-entry';
import { Injectable } from '@angular/core';
import 'rxjs/add/operator/map';
import { IDropboxAuth } from './idropbox-auth';
import { Http } from '@angular/http';
declare function require(name: string);

@Injectable()
export class DropboxListFilesService extends DropboxBase {

    private auth: IDropboxAuth;
    private ignoreCase = require("ignore-case");

    constructor(http: Http, settings: Settings, logginService: LoggingService) {
        super(http, settings, logginService);
    }

    listFiles(auth: IDropboxAuth, includeDeleted?: boolean): Promise<IDropboxEntry[]> {
        this.auth = auth;
        return this.listFolder(includeDeleted);
    }

    /**
     * create a list of changed files given the previous known state: compare that to the new state from listfiles
     */
    getChangedFiles(currentList: IDropboxEntry[], prevList: IDropboxEntry[]): IDropboxEntry[] {
        //compare with what was changed 
        let changedItems = currentList.filter(item => {
            var prevItem = prevList.find(x => this.ignoreCase.equals(x.name, item.name));
            //no previous item, and the remote item is not deleted: Then this is a changed item
            if (!prevItem) {
                if (!item.deleted) {
                    return true;
                } else {
                    return false;
                }
            }
            //if item revision is different, or it is deleted, it must haven been changed on the server
            return item.deleted || prevItem.rev !== item.rev;
        });
        return changedItems;
    }

    /**
     * get a list of all items from the remote PersonalWiki directory
     */
    private listFolder(includeDeleted?: boolean): Promise<IDropboxEntry[]> {
        let data = {
            path: "/PersonalWiki",
            include_deleted: includeDeleted
        };
        return new Promise<IDropboxEntry[]>((resolve, reject) => {
            this.doRequest("https://api.dropboxapi.com/2/files/list_folder", this.auth, data)
                .map(list => list.entries.map(entry => {
                    return <IDropboxEntry>{
                        deleted: entry[".tag"] === "deleted",
                        isFile: entry[".tag"] === "file",
                        name: entry.name,
                        id: entry.id,
                        rev: entry.rev
                    };
                })).subscribe(
                    (entries: IDropboxEntry[]) => resolve(entries),
                    err => reject(err)
                );
        });
    }
}