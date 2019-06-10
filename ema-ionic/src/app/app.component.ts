import { Component } from '@angular/core';

import { Platform } from '@ionic/angular';
import { SplashScreen } from '@ionic-native/splash-screen/ngx';
import { StatusBar } from '@ionic-native/status-bar/ngx';
import { Settings } from './library/settings';
import { TagIndexService } from './library/tag-index.service';
import { AppMinimize } from '@ionic-native/app-minimize/ngx';
import { PubSubService } from './library/pub-sub.service';

@Component({
    selector: 'app-root',
    templateUrl: 'app.component.html'
})
export class AppComponent {

    static instance: AppComponent;

    get themeBlue() { return this.theme === 'Blue'; }
    get themeDark() { return this.theme === 'Dark'; }
    get themeWood() { return this.theme === 'Wood'; }

    private theme = 'Blue';

    constructor(
        private platform: Platform,
        private pubsubService: PubSubService,
        private appMinimize: AppMinimize,
        private splashScreen: SplashScreen,
        private statusBar: StatusBar,
        private settings: Settings,
        private tagIndexService: TagIndexService) {
        AppComponent.instance = this;
        this.initializeApp();
    }

    async initializeApp() {
        await this.platform.ready();
        this.platform.backButton.subscribe(() => this.onBackButton());

        this.statusBar.overlaysWebView(false);
        await this.settings.waitForInitialize();

        this.theme = this.settings.getStyle();

        // build initial index if needed, don't wait for it.
        this.tagIndexService.buildInitialIndex();

        setTimeout(() => this.splashScreen.hide(), 500);
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
