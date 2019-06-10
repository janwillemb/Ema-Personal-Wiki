import { TagIndexService } from '../../library/tag-index.service';
import { WikiStorage } from '../../library/wiki-storage';
import { LoggingService } from '../../library/logging-service';
import { IDropboxAuth } from '../../library/idropbox-auth';
import { DropboxAuthService } from '../../library/dropbox-auth.service';
import { DropboxSyncService } from '../../library/dropbox-sync-service';
import { Settings } from '../../library/settings';
import { Component } from '@angular/core';

import { AlertController, LoadingController, NavController } from '@ionic/angular';

@Component({
    selector: 'page-settings',
    templateUrl: 'settings.html',
    styleUrls: ['settings.scss'],
})
export class SettingsPage {
    syncMinutes: number;
    autoSync: boolean;
    styleName: string;
    showSearch: boolean;
    restoreLast: boolean;
    useCurly: boolean;
    localWikiDirectory: string;
    fontSize: number;
    stayActiveInBackground: boolean;

    private styleNameOnStart: string;

    constructor(
        private alertController: AlertController,
        private settings: Settings,
        private wikiStorage: WikiStorage,
        private tagIndexService: TagIndexService,
        private loadingController: LoadingController,
        private loggingService: LoggingService,
        private syncService: DropboxSyncService,
        private authService: DropboxAuthService) {

        const syncMinutesValue = this.settings.getSyncMinutes();
        if (syncMinutesValue === 10) {
            this.syncMinutes = null;
        } else {
            this.syncMinutes = syncMinutesValue;
        }
        this.autoSync = this.settings.getAutoSync();
        this.fontSize = this.settings.getFontSize();
        this.showSearch = this.settings.getShowSearch();
        this.styleName = settings.getStyle();
        this.styleNameOnStart = this.styleName;
        this.restoreLast = settings.getRestoreLast();
        this.useCurly = settings.getUseCurly();
        this.localWikiDirectory = settings.getLocalWikiDirectory();
        this.stayActiveInBackground = settings.getStayActiveInBackground();
    }

    async ionViewWillLeave() {
        this.settings.setFontSize(this.fontSize);
        this.settings.setSyncMinutes(this.syncMinutes);
        this.settings.setShowSearch(this.showSearch);
        this.settings.setRestoreLast(this.restoreLast);
        this.settings.setUseCurly(this.useCurly);
        this.settings.setAutoSync(this.autoSync);

        this.changeWikiPath();

        if (this.styleNameOnStart !== this.styleName) {
            await this.settings.setStyle(this.styleName);
            window.location.reload();
        }
    }

    private async changeWikiPath() {
        const oldWikiDirectory = this.settings.getLocalWikiDirectory();
        if (oldWikiDirectory !== this.localWikiDirectory) {
            const loading = await this.loadingController.create({ message: 'Changing local wiki directory' });
            loading.present()
                .then(() => {
                    if (this.authService.hasAuthenticatedWithDropbox()) {
                        return this.authService.getDropboxAuthentication();
                    }
                    return Promise.resolve(null);
                })
                .then((auth: IDropboxAuth) => {
                    if (!auth) {
                        return Promise.resolve();
                    }
                    const syncInfo = this.syncService.syncFiles(auth);
                    return syncInfo.promise;
                })
                .catch((err) => {
                    this.loggingService.log('Error syncing before changeWikiPath', err);
                    return this.syncService.clearLocalSyncState();
                })
                .then(() => this.wikiStorage.move(oldWikiDirectory, this.localWikiDirectory))
                .catch(async (err) => {
                    this.loggingService.log('Error changing wiki path', err);
                    const alert = await this.alertController.create({
                        header: 'Move failed',
                        message: 'Moving the wiki directory failed. Please move the files manually.'
                    });
                    return alert.present().then(() => this.syncService.clearLocalSyncState());
                })
                .then(() => this.settings.setLocalWikiDirectory(this.localWikiDirectory))
                .then(() => loading.dismiss());
        }

    }

    async buildTagIndex() {
        const loading = await this.loadingController.create({ message: 'Rebuilding tag index...' });
        loading.present()
            .then(() => this.tagIndexService.buildIndex())
            .catch(async (err) => {
                this.loggingService.log('Error building tag index', err);
                const alert = await this.alertController.create({
                    header: 'Create tag index failed',
                    message: 'Creating the tag index failed.'
                });
                return alert.present();
            })
            .then(() => loading.dismiss());
    }

    async clearSyncInfo() {
        const confirm = await this.alertController.create({
            header: 'Forget Dropbox credentials',
            message: 'Do you really want to clear the Dropbox authentication data?',
            buttons: [{
                text: 'Yes',
                handler: () => {
                    this.settings.removeDropboxAuthInfo();
                }
            }, {
                text: 'No'
            }]
        });
        confirm.present();
    }
}
