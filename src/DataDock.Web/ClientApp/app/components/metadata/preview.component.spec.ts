import { provideRoutes } from '@angular/router';
import { TestBed, inject } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { HttpModule } from '@angular/http';
import { PreviewComponent } from './preview.component';
import { CsvFile } from '../../shared';
import { ImportHelperService } from '../../shared/import-helper.service';
import { MockImportHelperService } from '../../testing/mocks/import-helper.service.mock';

describe('Metadata: Preview Component', () => {

    let fixture;
    let component;

    beforeEach(() => {

        TestBed.configureTestingModule({
            imports: [RouterTestingModule, ReactiveFormsModule, HttpModule],
            declarations: [PreviewComponent],
            providers: [
                {provide: ImportHelperService, useClass: MockImportHelperService},
                provideRoutes([])
            ]}
        ).compileComponents();
    });

    it('form manager should have a file set', inject([ImportHelperService], (ihs: ImportHelperService) => {
        let fileStub = new CsvFile();
        fileStub.filename = 'test.csv';
        fileStub.data = [
            ['Column 1', 'Column 2', 'Column 3', 'Column 4'],
            ['Row 1, Cell 1', 'Row 1, Cell 2', 'Row 1, Cell 3', 'Row 1, Cell 4'],
            ['Row 2, Cell 1', 'Row 2, Cell 2', 'Row 2, Cell 3', 'Row 2, Cell 4']];
        ihs.setSource(fileStub, 'jen', 'repo', 'abc');

        fixture = TestBed.createComponent(PreviewComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();

        const spy = spyOn(ihs, 'setSource');
        fixture.detectChanges();

        fixture.whenStable()
            .then(() => {
                expect(ihs.csvFile).toBeTruthy();

                let x = fixture.nativeElement.querySelector('.message');
                //
                expect(x).toBeTruthy();
                expect(x.innerHTML).toContain('The file contains 2 rows of data, previewing 2 rows of data below:');

                let trs = fixture.nativeElement.querySelectorAll('tr');
                expect(trs).toBeTruthy();
                console.warn(trs);
                expect(trs.length).toBe(3);
            });
    }));

});
