import { Injectable } from '@angular/core';
import { Http, RequestOptions, Headers, Response, RequestMethod, Request } from '@angular/http';
import { Observable } from 'rxjs';
import { Globals } from '../globals';

@Injectable()
export class ApiService {

  constructor(private globals: Globals, private http: Http) {}

  post(formData: FormData) {
    let headers = new Headers();
    let options = new RequestOptions({
      method: RequestMethod.Post,
      url: `${this.globals.apiUrl}data`,
      headers: headers,
      body: formData
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
