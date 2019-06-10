import { Settings } from '../../library/settings';
import { LoggingService } from '../../library/logging-service';
import { Component } from '@angular/core';
import { NavController, ToastController } from '@ionic/angular';
import { Clipboard } from '@ionic-native/clipboard/ngx';

@Component({
  selector: 'page-logs',
  templateUrl: 'logs.html',
  styleUrls: ['logs.scss'],
})
export class LogsPage {
  logLines: string[];

  constructor(
    loggingService: LoggingService,
    private clipboard: Clipboard,
    private navCtrl: NavController,
    private toastController: ToastController) {

    this.logLines = loggingService.consumeLogLines();
  }

  onBackButton() {
    this.navCtrl.pop();
  }

  async toClipboard() {
    this.clipboard.copy(this.logLines.join('\n'));
    const toast = await this.toastController.create({
      message: 'Loglines copied to clipboard',
      duration: 3000,
      position: 'bottom'
    });
    toast.present();
  }
}
