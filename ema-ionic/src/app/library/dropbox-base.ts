import { LoggingService } from './logging-service';
import { Settings } from './settings';
import { Response } from '@angular/http';
import { IDropboxAuth } from './idropbox-auth';
import { Observable } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Utils } from './utils';

export class DropboxBase {

    constructor(
        private http: HttpClient,
        protected settings: Settings,
        private loggingService: LoggingService) {

    }

    initialize(auth: IDropboxAuth): Promise<any> {
        let headers = this.createHeader(auth);
        headers = headers.append('Content-Type', 'application/json');

        return new Promise((resolve, reject) => {
            this.add401catch(
                this.http.post('https://api.dropboxapi.com/2/files/get_metadata', { path: '/PersonalWiki' }, { headers }))
                .subscribe(
                    () => resolve(),
                    (err) => {
                        this.loggingService.log('error posting to dropbox api ' + Utils.serializeError(err));
                        // try to create
                        this.add401catch(this.http.post('https://api.dropboxapi.com/2/files/create_folder', null, { headers }))
                            .subscribe(() => resolve(), err2 => reject('Error creating PersonalWiki folder ' +
                                Utils.serializeError(err2)));
                    });
        });
    }

    private createHeader(auth: IDropboxAuth): HttpHeaders {
        let headers = new HttpHeaders();
        headers = headers.append('Authorization', 'Bearer ' + auth.accessToken);
        return headers;
    }

    protected doRequest(uri: string, auth: IDropboxAuth, data: any): Observable<any> {
        let headers = this.createHeader(auth);
        headers = headers.append('Content-Type', 'application/json');

        return this.add401catch(
            this.http.post(uri, data, { headers }));
    }

    private add401catch(obs: Observable<any>): Observable<any> {
        return obs.pipe(catchError((err) => {
            if (err.status === 401) {
                this.settings.removeDropboxAuthInfo();
            }
            throw err;
        }));
    }

    protected downloadFile(path: string, isText: boolean, auth: IDropboxAuth): Observable<string | ArrayBuffer> {

        let headers = this.createHeader(auth);
        headers = headers.append('Dropbox-API-Arg', this.stringifyApiArg({ path }));

        const url = 'https://content.dropboxapi.com/2/files/download';

        if (isText) {
            return this.add401catch(this.http.post(url, null, { headers, responseType: 'text' }));
        } else {
            return this.add401catch(this.http.post(url, null, { headers, responseType: 'arraybuffer' }));
        }
    }

    private stringifyApiArg(apiArg: any) {
        let json = JSON.stringify(apiArg);
        json = json.replace(/[\u007F-\uFFFF]/g, (chr) =>
            '\\u' + ('0000' + chr.charCodeAt(0).toString(16)).substr(-4)
        );
        return json;
    }

    protected uploadFile(path: string, contents: string | ArrayBuffer, auth: IDropboxAuth): Observable<any> {
        let headers = this.createHeader(auth);

        headers = headers.append('Dropbox-API-Arg', this.stringifyApiArg({
            path,
            mode: 'overwrite'
        }));
        headers = headers.append('Content-Type', 'application/octet-stream');

        const body = new Blob([contents]);
        return this.add401catch(
            this.http.post('https://content.dropboxapi.com/2/files/upload', body, { headers }));
    }
}
