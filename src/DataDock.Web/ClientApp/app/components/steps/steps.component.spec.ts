import { Component } from '@angular/core';
import { TestBed, inject, ComponentFixture } from '@angular/core/testing';
import { StepsComponent } from './steps.component';
import { Observable } from 'rxjs';
import { ActivatedRoute, provideRoutes } from '@angular/router';
import { RepositoryInfo } from '../shared/repository-info';
import { HttpModule } from '@angular/http';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterTestingModule } from '@angular/router/testing';
import { MockImportHelperService } from '../testing/mocks/import-helper.service.mock';
import { ImportHelperService } from '../shared/import-helper.service';

describe('Steps Component', () => {

    let testHostComponent: StepsTestComponent;
    let testHostFixture: ComponentFixture<StepsTestComponent>;

    let fixture;
    let component;

    beforeEach(() => {

        TestBed.configureTestingModule({
            imports: [RouterTestingModule, ReactiveFormsModule, HttpModule],
            declarations: [StepsComponent],
            providers: [
                provideRoutes([]),
                {
                    provide: ActivatedRoute,
                    useValue: {
                        params: Observable.of({ownerId: 'test-user', repoId: 'test-repo'})
                    }
                },
                {provide: ImportHelperService, useClass: MockImportHelperWithRepoSuccess},
            ]
        })
            .compileComponents();

        fixture = TestBed.createComponent(StepsComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();

    });

    it('should contain "step1" element', () => {
        let x = fixture.nativeElement.querySelector('#step1');
        expect(x).toBeTruthy();
    });

    it('#step1 should contain "Choose" text', () => {
        let x = fixture.nativeElement.querySelector('#step1');
        expect(x.innerHTML).toContain('Choose');
    });

    it('should contain "step2" element', () => {
        let x = fixture.nativeElement.querySelector('#step2');
        expect(x).toBeTruthy();
    });

    it('#step2 should contain "Define" text', () => {
        let x = fixture.nativeElement.querySelector('#step2');
        expect(x.innerHTML).toContain('Define');
    });

    it('should contain "step3" element', () => {
        let x = fixture.nativeElement.querySelector('#step3');
        expect(x).toBeTruthy();
    });

    it('#step3 should contain "Upload" text', () => {
        let x = fixture.nativeElement.querySelector('#step3');
        expect(x.innerHTML).toContain('Upload');
    });


    @Component({
        selector: `dd-steps-test`,
        template: `<dd-steps activeStep="define" isUploading="false" restartLink="relativeUrlToRepoImport"></dd-steps>`
    })
    class StepsTestComponent {
    }

    class MockImportHelperWithRepoSuccess extends MockImportHelperService {
        constructor() {
            super();
            let testRepo = new RepositoryInfo({
                RepositoryId: 'test-user/test-repo'
            });
            this.setTargetRepository(testRepo);
        }
    }
});
