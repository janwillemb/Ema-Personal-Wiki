import { LogsPage } from '../pages/logs/logs';
import { Component, ViewChild } from '@angular/core';
import { NavController, Platform } from 'ionic-angular';
import { StatusBar, Splashscreen } from 'ionic-native';

import { WikiPage } from '../pages/wiki/wiki';

@Component({
    templateUrl: 'app.html'
})
export class MyApp {
    rootPage = WikiPage;
    @ViewChild("navController") navController: NavController;

    constructor(platform: Platform) {
        platform.ready().then(() => {
            // Okay, so the platform is ready and our plugins are available.
            // Here you can do any higher level native things you might need.
            StatusBar.styleDefault();
            Splashscreen.hide();
        });
    }

    showLogs() {
        this.navController.push(LogsPage);
    } 

}
