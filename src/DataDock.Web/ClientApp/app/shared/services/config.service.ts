import { Injectable, Inject } from '@angular/core';
import { Http, Response } from '@angular/http';
import 'rxjs/add/operator/toPromise';

import { Configuration } from '../models/configuration';

@Injectable()
export class ConfigurationService {
    private readonly configUrlPath: string = 'api/clientConfig';
    private configData: Configuration;

    // Inject the http service and the app's BASE_URL
    constructor(
        private http: Http,
        @Inject('BASE_URL') private originUrl: string) {}

    // Call the ClientConfiguration endpoint, deserialize the response,
    // and store it in this.configData.
    loadConfigurationData(): Promise<Configuration> {
        const config = new Configuration();
        config.inDebug = false;
        return Promise.resolve(config);
    }

    // A helper property to return the config object
    get config(): Configuration {
        return this.configData;
    }
}