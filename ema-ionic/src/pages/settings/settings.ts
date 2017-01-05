import { WikiStorage } from '../../library/wiki-storage';
import { LoggingService } from '../../library/logging-service';
import { IDropboxAuth } from '../../library/idropbox-auth';
import { DropboxAuthService } from '../../library/dropbox-auth.service';
import { DropboxSyncService } from '../../library/dropbox-sync-service';
import { MyApp } from '../../app/app.component';
import { Settings } from '../../library/settings';
import { Component } from '@angular/core';

import { AlertController, LoadingController, NavController } from 'ionic-angular';

@Component({
  selector: 'page-settings',
  templateUrl: 'settings.html'
})
export class SettingsPage {
  syncMinutes: number;
  styleName: string;
  styleGrey: boolean;
  showSearch: boolean;
  restoreLast: boolean;
  useCurly: boolean;
  localWikiDirectory: string;

  constructor(
    private alertController: AlertController,
    private navCtrl: NavController,
    private settings: Settings,
    private wikiStorage: WikiStorage,
    private loadingController: LoadingController,
    private loggingService: LoggingService,
    private syncService: DropboxSyncService,
    private authService: DropboxAuthService) {

    var syncMinutesValue = this.settings.getSyncMinutes();
    if (syncMinutesValue === 10) {
      this.syncMinutes = null;
    } else {
      this.syncMinutes = syncMinutesValue;
    }

    this.showSearch = this.settings.getShowSearch();
    this.styleName = settings.getStyle();
    this.restoreLast = settings.getRestoreLast();
    this.useCurly = settings.getUseCurly();
    this.styleGrey = this.styleName === "Grey";
    this.localWikiDirectory = settings.getLocalWikiDirectory();
  }

  ionViewWillLeave() {
    this.settings.setSyncMinutes(this.syncMinutes);
    this.settings.setShowSearch(this.showSearch);
    this.settings.setRestoreLast(this.restoreLast);
    this.settings.setUseCurly(this.useCurly);
    this.settings.setStyle(this.styleName)
      .then(() => MyApp.instance.reloadStyle());

    this.changeWikiPath();
  }

  private changeWikiPath() {
    var oldWikiDirectory = this.settings.getLocalWikiDirectory();
    if (oldWikiDirectory !== this.localWikiDirectory) {
      var loading = this.loadingController.create({ content: "Changing local wiki directory" });
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
          var syncInfo = this.syncService.syncFiles(auth);
          return syncInfo.promise;
        })
        .catch((err) => {
          this.loggingService.log("Error syncing before changeWikiPath", err)
          return this.syncService.clearLocalSyncState();
        })
        .then(() => this.wikiStorage.move(oldWikiDirectory, this.localWikiDirectory))
        .catch((err) => {
          this.loggingService.log("Error changing wiki path", err);
          var alert = this.alertController.create({
            title: "Move failed",
            message: "Moving the wiki directory failed. Please move the files manually."
          });
          return alert.present().then(() => this.syncService.clearLocalSyncState());
        })
        .then(() => this.settings.setLocalWikiDirectory(this.localWikiDirectory))
        .then(() => loading.dismiss());
    }

  }

  clearSyncInfo() {
    this.settings.removeDropboxAuthInfo();
  }
}
