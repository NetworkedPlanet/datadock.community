import { Component, OnInit, Input } from '@angular/core';
import { ImportHelperService } from '../shared';

@Component({
    selector: 'dd-data-preview',
    templateUrl: 'preview.component.html',
    styleUrls: ['preview.component.css']
})
export class PreviewComponent implements OnInit {

    @Input() maxRows: number;

    data: any;
    loading: boolean;

    headerRow: Array<any>;
    dataRows: Array<Array<any>>;
    originRows: number;
    originPlural: string;
    printRows: number;
    printPlural: string;

    constructor(private ihs: ImportHelperService) {
        this.loading = true;
        this.maxRows = 100;
        this.printRows = 0;
        this.originRows = 0;
        this.originPlural = 's';
        this.printPlural = 's';
        this.headerRow = [];
        this.dataRows = [];
    }

    ngOnInit(): void {
        // load data
        this.data = this.ihs.csvFile.data;

        if (this.data) {
            this.headerRow = this.data[0];
        }
        this.originRows = this.data.length - 1;
        if (this.originRows === 1) {
            this.originPlural = '';
        }
        this.printRows = this.originRows < this.maxRows ? this.originRows : this.maxRows;
        if (this.printRows === 1) {
            this.printPlural = '';
        }

        this.dataRows = this.data.slice(1, (this.printRows + 1));
    }
}
