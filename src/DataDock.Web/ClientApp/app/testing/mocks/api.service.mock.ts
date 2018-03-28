import { Observable } from 'rxjs';

export class MockApiService {

    apiUrl = 'http://testing.env/api/';

    post() {
        // do nothing for now
        let apiResponse = {message: 'API called successfully'};
        let successResponse = { status: 200, json: apiResponse };
        return Observable.of(successResponse);
    }
}
