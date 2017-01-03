import { MyApp } from '../../app/app.component';
import { Settings } from '../../library/settings';
import { Component } from '@angular/core';

import { NavController } from 'ionic-angular';

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

  constructor(public navCtrl: NavController, private settings: Settings) {
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
  }

  ionViewWillLeave() {
    this.settings.setSyncMinutes(this.syncMinutes);
    this.settings.setShowSearch(this.showSearch);
    this.settings.setRestoreLast(this.restoreLast);
    this.settings.setUseCurly(this.useCurly);
    return this.settings.setStyle(this.styleName)
      .then(() => MyApp.instance.reloadStyle());
  }

  clearSyncInfo() {
    this.settings.removeDropboxAuthInfo();
  }
}
