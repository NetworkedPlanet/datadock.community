import { provideRoutes, ActivatedRoute } from '@angular/router';
import { TestBed, inject } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { HttpModule } from '@angular/http';

import { FileComponent } from './file.component';
import { FormManager } from '../../shared/form-manager';
import { MockFormManager } from '../../testing/mocks/form-manager.mock';
import { StepsComponent } from '../steps/steps.component';
import { Observable } from 'rxjs';
import { RepositoryInfo } from '../../shared/repository-info';
import { SchemaService } from '../../shared/schema.service';
import { SchemaHelperService } from '../../shared/schema-helper.service';
import { ViewModelHelperService } from '../../shared/viewmodel-helper.service';
import { DatatypeService } from '../../shared/datatype.service';
import { MockSchemaService } from '../../testing/mocks/schema.service.mock';
import { MockSchemaHelperService } from '../../testing/mocks/schema-helper.service.mock';
import { DashboardModel } from '../../shared/dashboard-model';
import { AppService } from '../../shared/app.service';
import { MockAppService } from '../../testing/mocks/app.service.mock';

describe('File Component', () => {

    let fixture;
    let component;

    beforeEach(() => {

        // TODO mock the new services
      TestBed.configureTestingModule({
        imports: [RouterTestingModule, ReactiveFormsModule, HttpModule],
        declarations: [FileComponent, StepsComponent],
        providers: [
            {provide: FormManager, useClass: MockFormManager},
            {provide: AppService, useClass: MockAppServiceWithRepoSuccess},
            provideRoutes([]),
            {
                provide: ActivatedRoute,
                useValue: {
                    params: Observable.of({ownerId: 'test-user', repoId: 'test-repo'})
                }
            },
            {provide: SchemaService, useClass: MockSchemaService},
            {provide: SchemaHelperService, useClass: MockSchemaHelperService},
            ViewModelHelperService,
            DatatypeService
        ]}
            ).compileComponents();

        fixture = TestBed.createComponent(FileComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should show file upload input ', () => {
        let x = fixture.nativeElement.querySelector('#upload');
        expect(x).not.toBeNull();
        expect(x).toBeTruthy();
    });

    it('should display error when file size is too big.', inject([AppService], (ihs: AppService) => {
        let fileInputStub = {
            target: {
                files: [
                    {
                        name: 'test.csv',
                        size: 5194304,
                        // size: 254743,
                        lastModifiedDate: 'Wed Nov 09 2016 14:42:05 GMT+0000 (GMT Standard Time)' }
                ]}
            };

        component.fileChangeEvent(fileInputStub);
        const spy = spyOn(ihs, 'setSource');
        fixture.detectChanges();

        fixture.whenStable()
            .then(() => {
                expect(spy.calls.count()).toBe(0);
                let x = fixture.nativeElement.querySelector('.message');
                expect(x).toBeTruthy();
                expect(x.innerHTML).toContain('File size is over the 4MB limit.');
            });
    }));


    class MockAppServiceWithRepoSuccess extends MockAppService {
        constructor() {
            super();
            let testRepo = new RepositoryInfo({
                RepositoryId: 'test-user/test-repo'
            });
            this.setTargetRepository(testRepo);

            // dash
            let dashModelJson = {
                'Area': 'import',
                'SubTitle': null,
                'ShowOwnerDropDown': true,
                'UserId': 'jennet',
                'RequestedOwnerId': 'jennet',
                'RequestedOwnerAvatar': null,
                'IsAdmin': true,
                'OwnerRepositoryIds': ['jennet/jen-local-dev', 'jennet/open-data'],
                'OwnerSelectionList':
                    { 'CurrentOwnerId': 'jennet',
                        'CurrentOwnerAvatarUrl': 'https://avatars1.githubusercontent.com/u/4940135?v=4',
                        'Owners': [
                            {'OwnerId': 'jennet', 'AvatarUrl': 'https://avatars1.githubusercontent.com/u/4940135?v=4'},
                            {'OwnerId': 'BristolOpenData', 'AvatarUrl': 'https://avatars2.githubusercontent.com/u/26275325?v=4'},
                            {'OwnerId': 'datadocktestorg', 'AvatarUrl': 'https://avatars2.githubusercontent.com/u/28590678?v=4'},
                            {'OwnerId': 'NetworkedPlanet', 'AvatarUrl': 'https://avatars1.githubusercontent.com/u/1202409?v=4'},
                            {'OwnerId': 'Tech4GoodBristol', 'AvatarUrl': 'https://avatars3.githubusercontent.com/u/26736102?v=4'}
                        ]},
                'RepositoryId': 'jennet/legendary-octo-spork',
                'RepositoryShortId': null,
                'ReturnUrl': null,
                'SchemaTitle': 'test schema title',
                'HasErrored': false,
                'Errors': []
            };
            let dashJsonString = JSON.stringify(dashModelJson);
            let dashboardModel = new DashboardModel(dashJsonString);
            this.setDashboardModel(dashboardModel);
        }
    }
});

describe('File Component Fail', () => {

    let fixture;
    let component;

    beforeEach(() => {

        TestBed.configureTestingModule({
            imports: [RouterTestingModule, ReactiveFormsModule, HttpModule],
            declarations: [FileComponent, StepsComponent],
            providers: [
                {provide: FormManager, useClass: MockFormManager},
                {provide: AppService, useClass: MockAppServiceWithRepoFail},
                provideRoutes([]),
                {
                    provide: ActivatedRoute,
                    useValue: {
                        params: Observable.of({ownerId: 'test-user', repoId: 'test-repo'})
                    }
                },
                {provide: SchemaService, useClass: MockSchemaService},
                {provide: SchemaHelperService, useClass: MockSchemaHelperService},
                ViewModelHelperService,
                DatatypeService
            ]}
        ).compileComponents();

        fixture = TestBed.createComponent(FileComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should hide file upload input as there is no repo match ', () => {
        let x = fixture.nativeElement.querySelector('#upload');
        expect(x).toBeNull();
    });

    it('should show warning as there is no repo match ', () => {
        let x = fixture.nativeElement.querySelector('.messages');
        expect(x).not.toBeNull();
        expect(x).toBeTruthy();
        expect(x.innerHTML).toContain('There is a problem matching your selected repository to the import page requested');
    });

    class MockAppServiceWithRepoFail extends MockAppService {
        constructor() {
            super();
            let testRepo = new RepositoryInfo({
                RepositoryId: 'test-user/some-other-repo'
            });
            this.setTargetRepository(testRepo);

            // dash
            let dashModelJson = {
                'Area': 'import',
                'SubTitle': null,
                'ShowOwnerDropDown': true,
                'UserId': 'jennet',
                'RequestedOwnerId': 'jennet',
                'RequestedOwnerAvatar': null,
                'IsAdmin': true,
                'OwnerRepositoryIds': ['jennet/jen-local-dev', 'jennet/open-data'],
                'OwnerSelectionList':
                    { 'CurrentOwnerId': 'jennet',
                        'CurrentOwnerAvatarUrl': 'https://avatars1.githubusercontent.com/u/4940135?v=4',
                        'Owners': [
                            {'OwnerId': 'jennet', 'AvatarUrl': 'https://avatars1.githubusercontent.com/u/4940135?v=4'},
                            {'OwnerId': 'BristolOpenData', 'AvatarUrl': 'https://avatars2.githubusercontent.com/u/26275325?v=4'},
                            {'OwnerId': 'datadocktestorg', 'AvatarUrl': 'https://avatars2.githubusercontent.com/u/28590678?v=4'},
                            {'OwnerId': 'NetworkedPlanet', 'AvatarUrl': 'https://avatars1.githubusercontent.com/u/1202409?v=4'},
                            {'OwnerId': 'Tech4GoodBristol', 'AvatarUrl': 'https://avatars3.githubusercontent.com/u/26736102?v=4'}
                        ]}
            };
            let dashJsonString = JSON.stringify(dashModelJson);
            let dashboardModel = new DashboardModel(dashJsonString);
            this.setDashboardModel(dashboardModel);
        }
    }
});
