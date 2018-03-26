import { async, getTestBed, TestBed, inject } from '@angular/core/testing';
import { Injector } from '@angular/core';
import {
    Response, ResponseOptions, HttpModule, BaseRequestOptions, Http, RequestMethod,
    ConnectionBackend
} from '@angular/http';
import { MockBackend, MockConnection } from '@angular/http/testing';
import { ApiService } from './api.service';


describe('ApiService', () => {

    // setup
    beforeEach(() => TestBed.configureTestingModule({
        imports: [ HttpModule ],
        providers: [
            ApiService,
            MockBackend,
            BaseRequestOptions,
            {
                provide: Http,
                useFactory: (backend: ConnectionBackend, options: BaseRequestOptions) => {
                    return new Http(backend, options);
                }, deps: [MockBackend, BaseRequestOptions]
            }]
    }));

    it('should construct', async(inject(
        [ApiService, MockBackend], (service, mockBackend) => {

            expect(service).toBeDefined();
        })));

    describe('#post', () => {
        const postResponse = {
            message: 'test api post completed ok',
        };

        it('should call the correct API URL', async(inject(
            [ApiService, MockBackend], (service, mockBackend) => {

                let connection;
                connection = mockBackend.connections.subscribe(c => {
                    connection = c;
                    if (IN_DEBUG) {
                        expect(c.request.url).toBe('http://localhost:4376/api/data');
                    } else {
                        expect(c.request.url).toBe('/api/data');
                    }
                    expect(connection.request.method).toBe(RequestMethod.Post, 'post() method is not of the expected method type (POST)');
                });

                const apiMethod = service.post();
                apiMethod.subscribe();

            })));

    });

});

