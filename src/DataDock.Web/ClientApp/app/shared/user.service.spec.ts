import { async, getTestBed, TestBed, inject } from '@angular/core/testing';
import { Injector } from '@angular/core';
import {
    Response, ResponseOptions, HttpModule, BaseRequestOptions, Http, RequestMethod,
    ConnectionBackend
} from '@angular/http';
import { MockBackend, MockConnection } from '@angular/http/testing';
import { UserService } from './user.service';


describe('UserService', () => {

    // setup
    beforeEach(() => TestBed.configureTestingModule({
        imports: [ HttpModule ],
        providers: [
            UserService,
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
        [UserService, MockBackend], (service, mockBackend) => {

            expect(service).toBeDefined();
        })));

    describe('#get', () => {
        const mockUser = {
            userId: 'jennet',
            defaultLanguage: 'en-GB'
        };

        it('should parse response', async(inject(
            [UserService, MockBackend], (service, mockBackend) => {

                mockBackend.connections.subscribe(conn => {
                    conn.mockRespond(new Response(new ResponseOptions({ status: 200, body: JSON.stringify(mockUser) })));
                });

                const result = service.get();

                result.subscribe(res => {
                    expect(res).toEqual({
                        status: 200,
                        json: {
                            userId: 'jennet',
                            defaultLanguage: 'en-GB'
                        }
                    });
                });
            })));

        it('should call the correct API URL', async(inject(
            [UserService, MockBackend], (service, mockBackend) => {

                let connection;
                connection = mockBackend.connections.subscribe(c => {
                   connection = c;
                   // expect(connection.request.url).toBe('DEFINITELY NOT THIS 1');
                   if (this.globals.config.inDebug) {
                       expect(c.request.url).toBe('http://localhost:4376/api/user');
                   } else {
                       expect(c.request.url).toBe('/api/user');
                   }
                    expect(connection.request.method).toBe(RequestMethod.Get, 'get() method is not of the expected method type (get)');
                });

                const apiMethod = service.get();
                apiMethod.subscribe();

            })));

    });

});

