import { Settings } from '../library/settings';
import { SettingsPage } from '../pages/settings/settings';
import { LogsPage } from '../pages/logs/logs';
import { Component, ViewChild } from '@angular/core';
import { MenuController, NavController, Platform } from 'ionic-angular';
import { StatusBar, Splashscreen } from 'ionic-native';

import { WikiPage } from '../pages/wiki/wiki';

@Component({
    templateUrl: 'app.html'
})
export class MyApp {
    rootPage: any;
    @ViewChild("navController") navController: NavController;
    styleGrey: boolean;
    static instance: MyApp;

    private unregisterBackButtonAction: Function;

    constructor(
        private platform: Platform,
        private settings: Settings,
        private menu: MenuController) {

        platform.ready().then(() => {
            StatusBar.styleDefault();

            settings.waitForInitialize().then(() => {
                this.rootPage = WikiPage;
                this.reloadStyle();
                setTimeout(() => Splashscreen.hide(), 100);
            });

            platform.pause.subscribe(() => this.unregisterBackButton());
            platform.resume.subscribe(() => this.registerBackButton());

            this.registerBackButton();
        });

        MyApp.instance = this;
    }

    private registerBackButton() {
        if (!this.unregisterBackButtonAction) {
            this.unregisterBackButtonAction = this.platform.registerBackButtonAction(() => this.onBackButton(), Number.MAX_VALUE);
        }
    }

    private unregisterBackButton() {
        if (this.unregisterBackButtonAction) {
            this.unregisterBackButtonAction();
            this.unregisterBackButtonAction = null;
        }
    }

    private onBackButton() {
        if (this.menu.isOpen()) {
            this.menu.close();
        } else {
            var activePage = this.navController.getActive();
            if (activePage.instance.onBackButton) {
                activePage.instance.onBackButton();
            } else {
                this.navController.pop();
            }
        }
    }

    reloadStyle() {
        this.styleGrey = this.settings.getStyle() === "Grey";
    }

    showLogs() {
        this.navController.push(LogsPage);
    }

    showSettings() {
        this.navController.push(SettingsPage);
    }

    exit() {
        this.platform.exitApp();
    }

    minimize() {
        this.unregisterBackButton(); //otherwise resuming will have an unresponsive backbutton
        var w: any = window;
        if (w && w.plugins && w.plugins.appMinimize && w.plugins.appMinimize.minimize) {
            w.plugins.appMinimize.minimize();
        } else {
            this.exit();
        }
    }

}
