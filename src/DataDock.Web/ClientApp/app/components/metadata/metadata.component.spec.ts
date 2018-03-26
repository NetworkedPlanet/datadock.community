import { ComponentFixture, TestBed, inject, async } from '@angular/core/testing';
import { By }              from '@angular/platform-browser';
import { DebugElement }    from '@angular/core';
import { FormsModule, ReactiveFormsModule, FormGroup } from '@angular/forms';

import { provideRoutes } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { TagInputModule } from 'ng2-tag-input';


import { MetadataComponent } from './metadata.component';
import { DatasetComponent } from './dataset.component';
import { ColumnDefinitionsComponent } from './column-definitions.component';
import { AdvancedComponent } from './advanced.component';
import { PreviewComponent } from './preview.component';
import { DeveloperComponent } from '../developer/developer.component';

import { FormManager } from '../shared/form-manager';
import { FormFieldComponent } from '../shared/form-field';
import { MockFormManager } from '../testing/mocks';
import { DatatypeService } from '../shared/datatype.service';

describe('MetadataComponent', () => {

    let fixture;
    let component;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [
                RouterTestingModule,
                ReactiveFormsModule,
                TagInputModule],
            declarations: [
                MetadataComponent,
                DatasetComponent,
                ColumnDefinitionsComponent,
                AdvancedComponent,
                PreviewComponent,
                DeveloperComponent,
                FormFieldComponent],
            providers: [
                DatatypeService,
                provideRoutes([]),
                { provide: FormManager, useValue: MockFormManager }]
        })
            .compileComponents();

        TestBed.overrideComponent(MetadataComponent, {
            set: {
                providers: [
                    // this will override the @Component.providers:[MyService]

                ]
            }
        });

        fixture = TestBed.createComponent(MetadataComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    xit('should have a defined component', () => {
        expect(component).toBeDefined();
    });

    xit('should create a `FormGroup` comprised of `FormControl`s', () => {
        component.ngOnInit();
        expect(component.form instanceof FormGroup).toBe(true);
    });

    xit('should display #step1 ', async(inject([], () => {
        fixture.whenStable()
            .then(() => {
                fixture.detectChanges();
                return fixture.whenStable();
            })
            .then(() => {
                const compiled = fixture.debugElement.nativeElement;
                let x = compiled.querySelector('#step1');
                expect(x).toBeTruthy();
                expect(x.innerHTML).toContain('Select');
            });
    })));

    xit('should contain "step2" element', () => {
        fixture.detectChanges();

        let x = fixture.nativeElement.querySelector('#step2');
        expect(x).toBeTruthy();
        expect(x.innerHTML).toContain('Choose');
    });

    xit('should contain "step3" element', () => {
        fixture.detectChanges();

        let x = fixture.nativeElement.querySelector('#step3');
        expect(x).toBeTruthy();
        expect(x.innerHTML).toContain('Define');
    });

    xit('should contain "step4" element', () => {
        fixture.detectChanges();

        let x = fixture.nativeElement.querySelector('#step4');
        expect(x).toBeTruthy();
        expect(x.innerHTML).toContain('Upload');
    });
});
