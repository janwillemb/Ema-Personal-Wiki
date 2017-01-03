import { Settings } from './settings';
import { Observable } from 'rxjs/Rx';
import { RequestOptionsArgs } from '@angular/http/src/interfaces';
import { Headers, Http, Response } from '@angular/http';
import { IDropboxAuth } from './idropbox-auth';
declare function require(name: string);

export class DropboxBase {
    private serializeError = require("serialize-error");

    constructor(private http: Http, protected settings: Settings) {

    }

    initialize(auth: IDropboxAuth): Promise<any> {
        var headers = this.createHeader(auth);
        headers.append("Content-Type", "application/json");

        let options: RequestOptionsArgs = {
            headers: headers,
            body: this.stringifyApiArg({
                path: "/PersonalWiki"
            })
        };

        return new Promise((resolve, reject) => {
            this.add401catch(this.http.post("https://api.dropboxapi.com/2/files/get_metadata", null, options))
                .map((response: Response) => response.json())
                .subscribe(
                result => resolve(),
                err => {
                    //try to create
                    this.add401catch(this.http.post("https://api.dropboxapi.com/2/files/create_folder", null, options))
                        .subscribe(() => resolve(), err => reject("Error creating PersonalWiki folder " + JSON.stringify(this.serializeError(err))));
                });
        });
    }

    private createHeader(auth: IDropboxAuth) {
        let headers = new Headers();
        headers.append("Authorization", "Bearer " + auth.accessToken);
        return headers;
    }

    protected doRequest(uri: string, auth: IDropboxAuth, data: any): Observable<any> {
        var headers = this.createHeader(auth);
        headers.append("Content-Type", "application/json");

        let options: RequestOptionsArgs = {
            headers: headers,
            body: this.stringifyApiArg(data)
        };
        return this.add401catch(
            this.http.post(uri, null, options)
                .map((response: Response) => response.json()));

    }

    private add401catch(obs: Observable<any>): Observable<any> {
        return obs.catch(err => {
            if (err.status === 401) {
                this.settings.removeDropboxAuthInfo();
            }
            throw err;
        });
    }

    protected downloadText(path: string, auth: IDropboxAuth): Observable<string> {

        var headers = this.createHeader(auth);
        headers.append("Dropbox-API-Arg", this.stringifyApiArg({
            path: path
        }));

        let options: RequestOptionsArgs = {
            headers: headers
        };

        return this.add401catch(
            this.http.post("https://content.dropboxapi.com/2/files/download", null, options)
                .map((response: Response) => response.text()));
    }

    private stringifyApiArg(apiArg: any) {
        var json = JSON.stringify(apiArg)
        json  = json.replace(/[\u007F-\uFFFF]/g, function(chr) {
            return "\\u" + ("0000" + chr.charCodeAt(0).toString(16)).substr(-4)
        });
        return json;
    }

    protected uploadText(path: string, contents: string, auth: IDropboxAuth): Observable<any> {
        var headers = this.createHeader(auth);

        headers.append("Dropbox-API-Arg", this.stringifyApiArg({
            path: path,
            mode: "overwrite"
        }));
        headers.append("Content-Type", "application/octet-stream");

        let body = new Blob([contents]);
        return this.add401catch(
            this.http.post("https://content.dropboxapi.com/2/files/upload", body, { headers: headers })
                .map((response: Response) => response.json()));
    }
}