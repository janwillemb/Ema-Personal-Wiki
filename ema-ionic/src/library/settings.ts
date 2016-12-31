import { Storage } from '@ionic/storage';
import { Injectable } from '@angular/core';
@Injectable()
export class Settings {
    remoteWikiDirectory: string = "/PersonalWiki/";

    constructor(private storage: Storage) {

    }

    getRemotePath(fileName: string): string {
        return this.remoteWikiDirectory + fileName;
    }

    getSyncMinutes(): Promise<number> {
        return this.storage.get("syncMinutes").then(value => {
            if (value === 0) {
                return 0
            }
            if (!value || isNaN(value)) {
                return 10;
            }
            return <number>value;
        });
    }

    setSyncMinutes(value: number): Promise<any> {
        return this.storage.set("syncMinutes", value);
    }

}