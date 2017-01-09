import { TagIndexService } from '../../library/tag-index.service';
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
  fontSize: number;
  stayActiveInBackground: boolean;

  constructor(
    private alertController: AlertController,
    private navCtrl: NavController,
    private settings: Settings,
    private wikiStorage: WikiStorage,
    private tagIndexService: TagIndexService,
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
    this.fontSize = this.settings.getFontSize();
    this.showSearch = this.settings.getShowSearch();
    this.styleName = settings.getStyle();
    this.restoreLast = settings.getRestoreLast();
    this.useCurly = settings.getUseCurly();
    this.styleGrey = this.styleName === "Grey";
    this.localWikiDirectory = settings.getLocalWikiDirectory();
    this.stayActiveInBackground = settings.getStayActiveInBackground();
  }

  ionViewWillLeave() {
    this.settings.setFontSize(this.fontSize);
    this.settings.setSyncMinutes(this.syncMinutes);
    this.settings.setShowSearch(this.showSearch);
    this.settings.setRestoreLast(this.restoreLast);
    this.settings.setUseCurly(this.useCurly);
    this.settings.setStyle(this.styleName)
      .then(() => MyApp.instance.reloadStyle());
    this.settings.setStayActiveInBackground(this.stayActiveInBackground)
      .then(() => MyApp.instance.configureBackgroundMode());

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

  buildTagIndex() {
    var loading = this.loadingController.create({ content: "Rebuilding tag index..." });
    loading.present()
      .then(() => this.tagIndexService.buildIndex())
      .catch(err => {
        this.loggingService.log("Error building tag index", err);
          var alert = this.alertController.create({
            title: "Create tag index failed",
            message: "Creating the tag index failed."
          });
          return alert.present();
      })
      .then(() => loading.dismiss());
  }

  clearSyncInfo() {
    let confirm = this.alertController.create({
      title: "Forget Dropbox credentials",
      message: "Do you really want to clear the Dropbox authentication data?",
      buttons: [{
        text: "Yes",
        handler: () => {
          this.settings.removeDropboxAuthInfo();
        }
      }, {
        text: "No"
      }]
    });
    confirm.present();
  }
}
