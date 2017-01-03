import { IDropboxAuth } from './idropbox-auth';
import { Storage } from '@ionic/storage';
import { Injectable } from '@angular/core';
@Injectable()
export class Settings {

    remoteWikiDirectory: string = "/PersonalWiki/";
    private initializePromise: Promise<any>;
    private syncMinutes: number;
    private style: string;
    private showSearch: boolean;
    private dropboxAuthInfo: IDropboxAuth;
    private lastPageName: string;
    private restoreLast: boolean;
    private useCurly: boolean;

    constructor(private storage: Storage) {
        this.initializePromise = this.initialize();
    }

    public waitForInitialize(): Promise<any> {
        return this.initializePromise;
    }

    private initialize(): Promise<any> {
        var promises = [];
        promises.push(this.storage.get("syncMinutes").then(value => this.syncMinutes = value));
        promises.push(this.storage.get("showSearch").then(value => this.showSearch = value));
        promises.push(this.storage.get("style").then(value => this.style = value));
        promises.push(this.storage.get("dropboxAuth").then(value => this.dropboxAuthInfo = value));
        promises.push(this.storage.get("lastPageName").then(value => this.lastPageName = value));
        promises.push(this.storage.get("restoreLast").then(value => this.restoreLast = value));
        promises.push(this.storage.get("useCurly").then(value => this.useCurly = value));
        return Promise.all(promises);
    }

    getRemotePath(fileName: string): string {
        return this.remoteWikiDirectory + fileName;
    }

    getLastPageName(): string {
        return this.lastPageName;
    }

    setLastPageName(value: string): Promise<any> {
        this.lastPageName = value;
        return this.storage.set("lastPageName", value);
    }

    getSyncMinutes(): number {
        var value = this.syncMinutes;
        if (value === 0) {
            return 0
        }
        if (!value || isNaN(value)) {
            return 10;
        }
        return <number>value;
    }

    setSyncMinutes(value: number): Promise<any> {
        if (typeof (value) === "string") {
            value = parseInt(value, 10);
        }
        this.syncMinutes = value;
        return this.storage.set("syncMinutes", value);
    }

    setRestoreLast(value: boolean): Promise<any> {
        this.restoreLast = value;
        return this.storage.set("restoreLast", value);
    }

    getRestoreLast(): boolean {
        var value = this.restoreLast;
        //default is true
        if (value === false) {
            return false;
        }
        return true;
    }

    setUseCurly(value: boolean): Promise<any> {
        this.useCurly = value;
        return this.storage.set("useCurly", value);
    }

    getUseCurly(): boolean {
        var value = this.useCurly;
        //default is true
        if (value === false) {
            return false;
        }
        return true;
    }
    
    setShowSearch(value: boolean): Promise<any> {
        this.showSearch = value;
        return this.storage.set("showSearch", value);
    }

    getShowSearch(): boolean {
        var value = this.showSearch;
        //default is true
        if (value === false) {
            return false;
        }
        return true;
    }

    getStyle(): string {
        if (this.style !== "Grey") {
            return "Wood";
        }
        return "Grey";
    }

    setStyle(value: string): Promise<any> {
        this.style = value;
        return this.storage.set("style", value);
    }

    getDropboxAuthInfo(): IDropboxAuth {
        return this.dropboxAuthInfo;
    }

    setDropboxAuthInfo(auth: IDropboxAuth): Promise<any> {
        this.dropboxAuthInfo = auth;
        return this.storage.set("dropboxAuth", auth);
    }

    removeDropboxAuthInfo(): Promise<any> {
        this.dropboxAuthInfo = null;
        return this.storage.remove("dropboxAuth");
    }

}