import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable()
export class MockUserService {
    apiUrl = 'http://testing.env/api/';

    get(): Observable<any> {
        let u = {
            userId: 'testuser'
        };
        let response = {
            status: 200,
            json: u
        };
        return Observable.of(response);
    }

    getRepository(): Observable<any> {
        let r = {
            name: 'Test Repo'
        };
        let response = {
            status: 200,
            json: r
        };
        return Observable.of(response);
    }
}
