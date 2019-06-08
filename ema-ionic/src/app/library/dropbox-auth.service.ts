import { Settings } from './settings';
import { LoggingService } from './logging-service';
import { Injectable, Injector } from '@angular/core';
import { IDropboxAuth } from './idropbox-auth';
import { InAppBrowser } from '@ionic-native/in-app-browser/ngx';
import { Storage } from '@ionic/storage';
import { Utils } from './utils';

@Injectable()
export class DropboxAuthService {

    constructor(
        private settings: Settings,
        private loggingService: LoggingService,
        private storage: Storage,
        private inAppBrowser: InAppBrowser) {
    }

    hasAuthenticatedWithDropbox(): boolean {
        return !!this.settings.getDropboxAuthInfo();
    }

    getDropboxAuthentication(): Promise<IDropboxAuth> {
        // for local testing in Ripple, the embedded browser solution won't work.
        // harvest an accesstoken from the device first (tip: Clipboard.copy (see below) -> mail to self)

        // import { Storage } from '@ionic/storage'; (in imports)
        // , private storage: Storage (in constructor)
        //
        // this.storage.set("dropboxAuth", {
        //     "accessToken": "<accesstoken>",
        //     "tokenType": "bearer", "uid": "<uid>", "accountId": "<accountid>"
        // });

        return new Promise<IDropboxAuth>((resolve, reject) => {

            const info = this.settings.getDropboxAuthInfo();
            if (info) {
                this.loggingService.log('Found Dropbox auth-info in localstorage');
                resolve(info);
            } else {
                this.loggingService.log('Didn\'t find Dropbox auth-info; ask user.');
                this.askDropboxPermission()
                    .then(rawUrl => this.parseDropboxReturnValue(rawUrl))
                    .then((dropboxAuth: IDropboxAuth) => {
                        // Clipboard.copy here
                        this.loggingService.log('parsed');
                        this.settings.setDropboxAuthInfo(dropboxAuth);
                        resolve(dropboxAuth);
                    })
                    .catch((err) => reject('Error asking for Dropbox info: ' + Utils.serializeError(err)));
            }
        });
    }

    private parseDropboxReturnValue(url: string): Promise<IDropboxAuth> {
        const re = /access_token=(.+)&token_type=(.+)&uid=(.+)&account_id=(.+)$/;
        const m = re.exec(url);
        this.loggingService.log('parsing');
        if (!m) {
            return Promise.reject('Not authenticated: ' + url);
        } else {
            return Promise.resolve({
                accessToken: m[1],
                tokenType: m[2],
                uid: m[3],
                accountId: m[4]
            });
        }
    }

    private askDropboxPermission(): Promise<any> {
        return new Promise((resolve, reject) => {
            if (!this.inAppBrowser) {
                reject('No inAppBrowser available');
            }

            try {
                const browser = this.inAppBrowser.create('https://www.dropbox.com/oauth2/authorize?' +
                    'response_type=token&' +
                    'client_id=l8tliwhtfvkrxl7&' +
                    'redirect_uri=http://127.0.0.1/');


                const subscription = browser.on('loadstart').subscribe(event => {
                    const rawUrl = event.url;
                    this.loggingService.log('loadstart ' + rawUrl);
                    if (rawUrl && rawUrl.startsWith('http://127.0.0.1')) {
                        // this is the callback
                        browser.close();
                        subscription.unsubscribe();
                        resolve(rawUrl);
                    }
                });

            } catch (ex) {
                this.loggingService.log('exception', ex);

                reject('ERROR: ' + Utils.serializeError(ex));
            }
        });
    }
}
