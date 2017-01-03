import { Settings } from '../../library/settings';
import { LoggingService } from '../../library/logging-service';
import { Component } from '@angular/core';
import { Clipboard } from 'ionic-native';
import { NavController, ToastController } from 'ionic-angular';

@Component({
  selector: 'page-logs',
  templateUrl: 'logs.html'
})
export class LogsPage {
  logLines: string[];
  styleGrey: boolean;

  constructor(
    settings: Settings,
    loggingService: LoggingService,
    private navCtrl: NavController,
    private toastController: ToastController) {

    this.logLines = loggingService.consumeLogLines();
    this.styleGrey = settings.getStyle() === "Grey";
  }

  onBackButton() {
    this.navCtrl.pop();
  }

  toClipboard() {
    Clipboard.copy(this.logLines.join("\n"));
    var toast = this.toastController.create({
      message: "Loglines copied to clipboard",
      duration: 3000,
      position: "bottom"
    })
    toast.present();
  }
}
