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
        .map(success => success.status)
        .catch(this.handleError);
  }

  private handleError (error: Response | any) {
    console.log('API error');
    let errMsg: string;
    if (error instanceof Response) {
      const body = error.json() || '';
      const err = body.error || JSON.stringify(body);
      errMsg = `${error.status} - ${error.statusText || ''} ${err}`;
    } else {
      errMsg = error.message ? error.message : error.toString();
    }
    console.error(errMsg);
    return Observable.throw(errMsg);
  }


}
