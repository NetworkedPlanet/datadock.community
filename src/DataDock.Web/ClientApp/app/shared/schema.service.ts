import { Injectable } from '@angular/core';
import { Http, RequestOptions, Headers, Response, RequestMethod, Request, URLSearchParams } from '@angular/http';
import { Observable } from 'rxjs';
import { Globals } from '../globals';

@Injectable()
export class SchemaService {

    constructor(private globals: Globals, private http: Http) {}

    // get current logged in user
    get(ownerId: string, schemaId: string) {
        let headers = new Headers();
        const params = new URLSearchParams();
        params.set('ownerId', ownerId);
        params.set('schemaId', schemaId);
        let options = new RequestOptions({
            method: RequestMethod.Get,
            url: `${this.globals.apiUrl}schema`,
            params: params,
            headers: headers,
        });
        return this.http.request(new Request(options))
            .map((res: Response) => {
                if (res) {
                    // TODO catch bad request etc
                    return { status: res.status, schemaInfo: res.json() };
                }
            });
    }

    private handleError (error: Response) {
        // TODO send the error to logging infrastructure instead of just logging it to the console
        console.error(error);
        return Observable.throw(error.json().error || 'Server error');
    }
}
