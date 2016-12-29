import { Injectable } from '@angular/core';
@Injectable()
export class Settings {
    remoteWikiDirectory: string = "/PersonalWiki/";

    getRemotePath(fileName: string): string {
        return this.remoteWikiDirectory + fileName;
    }
}