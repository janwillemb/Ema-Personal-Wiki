import { DropboxBase } from './dropbox-base';
import { IDropboxEntry } from './idropbox-entry';
import { Injectable } from '@angular/core';
import 'rxjs/add/operator/map';
import { IDropboxAuth } from './idropbox-auth';
import { Http } from '@angular/http';

@Injectable()
export class DropboxListFilesService extends DropboxBase {

    private auth: IDropboxAuth;

    constructor(http: Http) {
        super(http);
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
            var prevItem = prevList.find(x => x.id === item.id);
            //no previous item? Then this is a changed item
            if (!prevItem) {
                return true;
            }
            //if item revision is different, it must haven been changed on the server
            return prevItem.rev !== item.rev;
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