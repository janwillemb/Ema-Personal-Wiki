import { Settings } from './settings';
import { LoggingService } from './logging-service';
import { Injectable } from '@angular/core';
import { InAppBrowser } from 'ionic-native';
import { IDropboxAuth } from "./idropbox-auth";
import { Storage } from '@ionic/storage';
declare function require(name: string);

@Injectable()
export class DropboxAuthService {

    private serializeError = require("serialize-error");

    constructor(private settings: Settings, private loggingService: LoggingService, private storage: Storage) {

    }

    hasAuthenticatedWithDropbox(): boolean {
        return !!this.settings.getDropboxAuthInfo();
    }

    getDropboxAuthentication(): Promise<IDropboxAuth> {
        //for local testing in Ripple, the embedded browser solution won't work.
        //harvest an accesstoken from the device first (tip: Clipboard.copy (see below) -> mail to self)

        //import { Storage } from '@ionic/storage'; (in imports)
        //, private storage: Storage (in constructor)
        // 
        // this.storage.set("dropboxAuth", {
        //     "accessToken": "<accesstoken>", 
        //     "tokenType": "bearer", "uid": "<uid>", "accountId": "<accountid>"
        // });

        return new Promise<IDropboxAuth>((resolve, reject) => {
            var info = this.settings.getDropboxAuthInfo();
            if (info) {
                this.loggingService.log("Found Dropbox auth-info in localstorage");
                resolve(info);
            } else {
                this.loggingService.log("Didn't find Dropbox auth-info; ask user.");
                this.askDropboxPermission()
                    .then(rawUrl => this.parseDropboxReturnValue(rawUrl))
                    .then((dropboxAuth: IDropboxAuth) => {
                        //Clipboard.copy here
                        this.settings.setDropboxAuthInfo(dropboxAuth);
                        resolve(dropboxAuth);
                    })
                    .catch((err) => reject("Error asking for Dropbox info: " + JSON.stringify(this.serializeError(err))));
            }
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
                reject("ERROR: " + JSON.stringify(this.serializeError(ex)));
            }
        });
    }
}