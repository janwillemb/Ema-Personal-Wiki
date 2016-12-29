import { Observable } from 'rxjs/Rx';
import { RequestOptionsArgs } from '@angular/http/src/interfaces';
import { Headers, Http, Response } from '@angular/http';
import { IDropboxAuth } from './idropbox-auth';
export class DropboxBase {

    constructor(private http: Http) {

    }

    protected doRequest(uri: string, auth: IDropboxAuth, data: any): Observable<any> {
        let headers = new Headers();
        headers.append("Authorization", "Bearer " + auth.accessToken);
        headers.append("Content-Type", "application/json");

        let options: RequestOptionsArgs = {
            headers: headers,
            body: JSON.stringify(data)
        };
        return this.http.post(uri, null, options)
            .map((response: Response) => response.json());
    }

    protected downloadText(path: string, auth: IDropboxAuth): Observable<string> {
        var data = {
            path: path
        };

        let headers = new Headers();
        headers.append("Authorization", "Bearer " + auth.accessToken);
        headers.append("Dropbox-API-Arg", JSON.stringify(data));

        let options: RequestOptionsArgs = {
            headers: headers
        };

        return this.http.post("https://content.dropboxapi.com/2/files/download", null, options)
            .map((response: Response) => response.text());
    }

    protected uploadText(path: string, contents: string, auth: IDropboxAuth): Observable<any> {
        let headers = new Headers();
        headers.append("Authorization", "Bearer " + auth.accessToken);
        headers.append("Dropbox-API-Arg", JSON.stringify({
            path: path,
            mode: "overwrite"
        }));
        headers.append("Content-Type", "application/octet-stream");

        let body = new Blob([contents]);
        return this.http.post("https://content.dropboxapi.com/2/files/upload", body, {
            headers: headers
        }).map((response: Response) => response.json());
    }
}