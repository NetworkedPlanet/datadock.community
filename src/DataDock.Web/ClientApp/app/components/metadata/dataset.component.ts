import { Component, Input, OnInit } from '@angular/core';
import { FormControl } from '@angular/forms';
import { FormManager, ViewModelSection } from '../../shared';
import { OPTIONS_LICENSES } from './options-licenses';


@Component({
    selector: 'dd-dataset-info',
    templateUrl: 'dataset.component.html',
    styleUrls: ['dataset.component.css']
})
export class DatasetComponent implements OnInit {

    @Input() sectionName: string;
    section: ViewModelSection;

    licenses = OPTIONS_LICENSES;
    licenseField;

    constructor(private fm: FormManager) {
    }

    ngOnInit(): void {
        this.licenseField = this.fm.getField('license');
        this.configureLicenseField();
    }

    configureLicenseField() {
        if (this.licenseField) {
            let licenseField = this.licenseField[0];
            if (licenseField) {
                let licenseControl: FormControl = this.licenseField[1];
                licenseField.options = this.licenses;
            }
        }
    }

}
