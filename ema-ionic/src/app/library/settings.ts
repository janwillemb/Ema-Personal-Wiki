import { IDropboxAuth } from './idropbox-auth';
import { Storage, StorageConfig } from '@ionic/storage';
import { Injectable, Injector } from '@angular/core';

@Injectable()
export class Settings {

    private remoteWikiDirectory = '/PersonalWiki/';
    private localWikiDirectory: string;
    private initializePromise: Promise<any>;
    private syncMinutes: number;
    private autoSync: boolean;
    private style: string;
    private showSearch: boolean;
    private dropboxAuthInfo: IDropboxAuth;
    private lastPageName: string;
    private restoreLast: boolean;
    private useCurly: boolean;
    private fontSize: number;
    private stayActiveInBackground: boolean;

    constructor(private storage: Storage) {
        this.initializePromise = this.initialize();
    }

    public waitForInitialize(): Promise<any> {
        return this.initializePromise;
    }

    private initialize(): Promise<any> {
        const promises = [];
        promises.push(this.storage.get('syncMinutes').then(value => this.syncMinutes = value));
        promises.push(this.storage.get('autoSync').then(value => this.autoSync = value));
        promises.push(this.storage.get('showSearch').then(value => this.showSearch = value));
        promises.push(this.storage.get('style').then(value => this.style = value));
        promises.push(this.storage.get('dropboxAuth').then(value => this.dropboxAuthInfo = value));
        promises.push(this.storage.get('lastPageName').then(value => this.lastPageName = value));
        promises.push(this.storage.get('restoreLast').then(value => this.restoreLast = value));
        promises.push(this.storage.get('useCurly').then(value => this.useCurly = value));
        promises.push(this.storage.get('localWikiDirectory').then(value => this.localWikiDirectory = value));
        promises.push(this.storage.get('fontSize').then(value => this.fontSize = value));
        promises.push(this.storage.get('stayActiveInBackground').then(value => this.stayActiveInBackground = value));
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
        return this.storage.set('lastPageName', value);
    }

    getLocalWikiDirectory(): string {
        return this.localWikiDirectory || 'PersonalWiki';
    }

    setLocalWikiDirectory(value: string): Promise<any> {
        this.localWikiDirectory = value;
        return this.storage.set('localWikiDirectory', value);
    }

    getSyncMinutes(): number {
        const value = this.syncMinutes;
        if (value === 0) {
            return 0;
        }
        if (!value || isNaN(value)) {
            return 10;
        }
        return value as number;
    }

    setSyncMinutes(value: number): Promise<any> {
        if (typeof (value) === 'string') {
            value = parseInt(value, 10);
        }
        this.syncMinutes = value;
        return this.storage.set('syncMinutes', value);
    }

    getFontSize(): number {
        const value = this.fontSize;
        if (!value || isNaN(value)) {
            return 100;
        }
        return value as number;
    }

    setFontSize(value: number): Promise<any> {
        if (typeof (value) === 'string') {
            value = parseInt(value, 10);
        }
        this.fontSize = value;
        return this.storage.set('fontSize', value);
    }

    setRestoreLast(value: boolean): Promise<any> {
        this.restoreLast = value;
        return this.storage.set('restoreLast', value);
    }

    getRestoreLast(): boolean {
        const value = this.restoreLast;
        // default is true
        if (value === false) {
            return false;
        }
        return true;
    }

    setStayActiveInBackground(value: boolean): Promise<any> {
        this.stayActiveInBackground = value;
        return this.storage.set('stayActiveInBackground', value);
    }

    getStayActiveInBackground(): boolean {
        const value = this.stayActiveInBackground;
        // default is true
        if (value === false) {
            return false;
        }
        return true;
    }

    setUseCurly(value: boolean): Promise<any> {
        this.useCurly = value;
        return this.storage.set('useCurly', value);
    }

    getUseCurly(): boolean {
        const value = this.useCurly;
        // default is true
        if (value === false) {
            return false;
        }
        return true;
    }

    setShowSearch(value: boolean): Promise<any> {
        this.showSearch = value;
        return this.storage.set('showSearch', value);
    }

    getShowSearch(): boolean {
        const value = this.showSearch;
        // default is true
        if (value === false) {
            return false;
        }
        return true;
    }

    getAutoSync(): boolean {
        const value = this.autoSync;
        // default is true
        if (value === false) {
            return false;
        }
        return true;
    }

    setAutoSync(value: boolean): Promise<any> {
        this.autoSync = value;
        return this.storage.set('autoSync', value);
    }

    getStyle(): string {
        const acceptedStyles = ['Wood', 'Blue', 'Dark'];

        if (acceptedStyles.indexOf(this.style) === -1) {
            return 'Blue';
        }
        return this.style;
    }

    setStyle(value: string): Promise<any> {
        this.style = value;
        return this.storage.set('style', value);
    }

    getDropboxAuthInfo(): IDropboxAuth {
        return this.dropboxAuthInfo;
    }

    setDropboxAuthInfo(auth: IDropboxAuth): Promise<any> {
        this.dropboxAuthInfo = auth;
        return this.storage.set('dropboxAuth', auth);
    }

    removeDropboxAuthInfo(): Promise<any> {
        this.dropboxAuthInfo = null;
        return this.storage.remove('dropboxAuth');
    }

}
