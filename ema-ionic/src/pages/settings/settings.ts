import { Settings } from '../../library/settings';
import { Component } from '@angular/core';

import { NavController } from 'ionic-angular';

@Component({
  selector: 'page-settings',
  templateUrl: 'settings.html'
})
export class SettingsPage {
  syncMinutes: number;

  constructor(public navCtrl: NavController, private settings: Settings) {
    this.settings.getSyncMinutes().then(value => {
      if (value === 10) {
        this.syncMinutes = null;
      } else {
        this.syncMinutes = value;
      }
    });
  }

  ionViewDidLeave() {
    return this.settings.setSyncMinutes(this.syncMinutes);
  }
}
