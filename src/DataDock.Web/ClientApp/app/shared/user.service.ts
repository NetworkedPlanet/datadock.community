import { Injectable } from '@angular/core';
import { Http, RequestOptions, Headers, Response, RequestMethod, Request } from '@angular/http';
import { Observable } from 'rxjs';

    @Injectable()
    export class UserService {

    apiUrl = API_URL;

    constructor(private http: Http) {}

    // get current logged in user
    get() {
        let headers = new Headers();
        let options = new RequestOptions({
          method: RequestMethod.Get,
          url: `${this.apiUrl}user`,
          headers: headers,
        });
        return this.http.request(new Request(options))
        .map((res: Response) => {
          if (res) {
            return { status: res.status, json: res.json() };
          }
        });
    }

    private handleError (error: Response) {
    // TODO send the error to logging infrastructure instead of just logging it to the console
        console.error(error);
        return Observable.throw(error.json().error || 'Server error');
    }
}
