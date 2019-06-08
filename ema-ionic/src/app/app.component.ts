import { Component } from '@angular/core';

import { Platform, MenuController, NavController } from '@ionic/angular';
import { SplashScreen } from '@ionic-native/splash-screen/ngx';
import { StatusBar } from '@ionic-native/status-bar/ngx';
import { Settings } from './library/settings';
import { LoggingService } from './library/logging-service';
import { TagIndexService } from './library/tag-index.service';
import { AppMinimize } from '@ionic-native/app-minimize/ngx';
import { Subscription } from 'rxjs';
import { PubSubService } from './library/pub-sub.service';

@Component({
    selector: 'app-root',
    templateUrl: 'app.component.html'
})
export class AppComponent {

    static instance: AppComponent;

    styleGrey = true;

    private backbuttonSubscription: Subscription;

    constructor(
        private platform: Platform,
        private pubsubService: PubSubService,
        private appMinimize: AppMinimize,
        private splashScreen: SplashScreen,
        private statusBar: StatusBar,
        private settings: Settings,
        private loggingService: LoggingService,
        private tagIndexService: TagIndexService) {
        AppComponent.instance = this;
        this.initializeApp();
    }

    initializeApp() {
        this.platform.ready().then(() => {
            this.statusBar.overlaysWebView(false);
            this.settings.waitForInitialize().then(() => {
                this.styleGrey = this.settings.getStyle() === 'Blue';
                setTimeout(() => this.splashScreen.hide(), 500);
            });
        });

        this.backbuttonSubscription = this.platform.backButton.subscribe(() => this.onBackButton());

        // build initial index if needed, don't wait for it.
        this.tagIndexService.buildInitialIndex();
    }

    private onBackButton() {
        this.pubsubService.publish('back');
    }

    startSync() {
        this.pubsubService.publish('sync');
    }

    exit() {
        // tslint:disable-next-line: no-string-literal
        navigator['app'].exitApp();
    }

    minimize() {
        this.appMinimize.minimize();
    }
}
