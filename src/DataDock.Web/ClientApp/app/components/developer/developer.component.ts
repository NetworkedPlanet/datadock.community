import { Component } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { FormManager } from '../../shared/form-manager';
import { MetadataViewModel } from '../../shared/metadata-viewmodel';
import { Globals } from '../../globals';

@Component({
    selector: 'dd-dev',
    templateUrl: './developer.component.html',
    styleUrls: ['./developer.component.css']
})
export class DeveloperComponent  {

    devMode: boolean;
    form: FormGroup;
    viewModel: MetadataViewModel;

    constructor( public fm: FormManager, private globals: Globals) {
        if (this.globals.config.inDebug) {
            this.devMode = true;
            this.form = this.fm.mainForm;
            this.viewModel = this.fm.metadataViewModel;
        } else {
            this.devMode = false;
            this.form = new FormGroup({});
            this.viewModel = new MetadataViewModel();
        }
    }
}
