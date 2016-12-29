import { LoggingService } from './logging-service';
import { Injectable } from '@angular/core';
import { InAppBrowser } from 'ionic-native';
import { Storage } from '@ionic/storage';

import { IDropboxAuth } from "./idropbox-auth";

@Injectable()
export class DropboxAuthService {

    constructor(private storage: Storage, private loggingService: LoggingService) {

    }

    getDropboxAuthentication(): Promise<IDropboxAuth> {
        //for local testing in Ripple, the embedded browser solution won't work.
        //harvest an accesstoken from the device first (tip: Clipboard.copy (see below) -> mail to self)
        // this.storage.set("dropboxAuth", { "accessToken": "(my glorious accesstoken here)", "tokenType": "bearer", "uid": "my uid here", "accountId": "my account id here" })

        return this.storage.get("dropboxAuth")
            .then(value => {
                if (!value) {
                    throw "no value";
                }
                this.loggingService.log("Found Dropbox auth-info in localstorage");
                return value;
            })
            .catch(() => {
                this.loggingService.log("Didn't find Dropbox auth-info; ask user.");
                return this.askDropboxPermission()
                    .then(rawUrl => this.parseDropboxReturnValue(rawUrl))
                    .then((dropboxAuth: IDropboxAuth) => {
                        //Clipboard.copy here
                        this.storage.set("dropboxAuth", dropboxAuth);
                        return dropboxAuth;
                    });
            });
    }

    private parseDropboxReturnValue(url: string): Promise<IDropboxAuth> {
        var re = /access_token=(.+)&token_type=(.+)&uid=(.+)&account_id=(.+)$/;
        var m = re.exec(url);
        if (!m) {
            return Promise.reject("Not authenticated: " + url);
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
            try {
                let browser = new InAppBrowser("https://www.dropbox.com/oauth2/authorize?" +
                    "response_type=token&" +
                    "client_id=l8tliwhtfvkrxl7&" +
                    "redirect_uri=http://127.0.0.1/");

                var subscription = browser.on("loadstart").subscribe(event => {
                    var rawUrl = event.url;
                    if (rawUrl && rawUrl.startsWith("http://127.0.0.1")) {
                        //this is the callback
                        browser.close();
                        subscription.unsubscribe();
                        resolve(rawUrl);
                    }
                });
            } catch (ex) {
                reject("ERROR: " + JSON.stringify(ex));
            }
        });
    }
}