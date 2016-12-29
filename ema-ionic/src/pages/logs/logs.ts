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

  constructor(public navCtrl: NavController, loggingService: LoggingService, private toastController: ToastController) {
    this.logLines = loggingService.consumeLogLines();
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
