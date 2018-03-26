import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { FormGroup, FormControl, FormArray } from '@angular/forms';

import { FormField } from '../../shared/form-field/form-field';
import { ImportHelperService, FormManager, SchemaHelperService } from '../../shared';

@Component({
    selector: 'dd-metadata',
    templateUrl: 'metadata.component.html',
    styleUrls: ['metadata.component.css']
})
export class MetadataComponent implements OnInit {

    form: FormGroup;
    metadata: any;

    isLoading: boolean;
    isUploading: boolean;
    viewTab: string;
    restartLink: string;

    results: any;
    warnings: Array<string>;
    errors: Array<string>;

    datasetInfo = 'base';
    colDef = 'cols_basic';
    colDefAdvanced = 'cols_advanced';

    developerMode: boolean;

    showOnHomePage: boolean;
    saveAsSchema: boolean;
    schemaTitle: string;

    constructor(private router: Router, private ihs: ImportHelperService,
                private fm: FormManager, private shs: SchemaHelperService) {

        this.developerMode = IN_DEBUG;
        this.isLoading = false;
        this.isUploading = false;
        this.showOnHomePage = true;
        this.saveAsSchema = false;
        this.warnings = [];
        this.errors = [];

        if (IN_DEBUG) {
            console.log('Loading metadata editor');
        }

        // subscribe to warnings and errors from child components
        fm.warningAdded$.subscribe(
            message => {
                this.warnings.push(`${message}`);
            });
        fm.errorAdded$.subscribe(
            message => {
                this.errors.push(`${message}`);
            });
    }

    ngOnInit(): void {
        if (!this.fm.metadataViewModel) {
            console.error('SOMETHING IS HORRIBLY WRONG! :D');
            // todo display error
        } else {
            this.form = this.generateForm();

            this.form.valueChanges.subscribe((event) => this.displayValidationMessages());
            this.displayValidationMessages();
            this.restartLink = this.ihs.restartImportRelativeUrl;

            if (this.shs.hasSchema) {
                this.schemaTitle = this.shs.schemaTitle;
            }
        }
    }

    getTabChange(activeTab) {
        this.viewTab = activeTab;
    }

    setDatasetDisplay() {
        this.fm.setHomePageDisplay(this.showOnHomePage);
    }

    setSaveSchema() {
        this.fm.setSaveTemplateDisplay(this.saveAsSchema);
    }

    displayValidationMessages() {
        this.errors = [];
        let validationMessages = [];
        if (this.form) {
            let tab1 = <FormArray> this.form.controls['base'];
            let tab2 = <FormArray> this.form.controls['cols_basic'];
            let tab3 = <FormArray> this.form.controls['cols_advanced'];
            let tab1Errors = this.checkFieldsValid(tab1);
            if (tab1Errors.length > 0) {
                validationMessages = validationMessages.concat(tab1Errors);
            }
            let tab2Errors = this.checkFieldsValid(tab2);
            if (tab2Errors.length > 0) {
                validationMessages = validationMessages.concat(tab2Errors);
            }
            let tab3Errors = this.checkFieldsValid(tab3);
            if (tab3Errors.length > 0) {
                validationMessages = validationMessages.concat(tab3Errors);
            }
            if (validationMessages.length > 0) {
                for (let msg of validationMessages) {
                    let msgSplit = msg.replace(/_/g, ' ');
                    this.errors.push(msgSplit);
                }
            }
        }
    }

    getErrorMessage(ctrlName: string, errorType: string): string {
        let validationMessage: string;
        if (ctrlName !== '' && errorType !== '') {
            let field = this.fm.getField(ctrlName);
            if (field) {
                let f = field[0] as FormField;
                let c = field[1] as FormControl;
                if (c.errors && !c.valid) {
                    for (let v of f.validations) {
                        if (v['type'] === errorType.toLowerCase()) {
                            validationMessage = v['message'];
                        }
                    }
                    if (validationMessage === '') {
                        // check custom validators
                        for (let cv of f.customValidations) {
                            if (cv['type'] === errorType.toLowerCase()) {
                                validationMessage = cv['message'];
                            }
                        }
                    }
                }
            }
        }
        return validationMessage;
    }

    checkFieldsValid(section: FormArray): Array<string> {
        let validationMessages = [];
        if (section) {
            for (let fg of section.controls) {
                let group: FormGroup = fg as FormGroup;
                for (let ctrlName of Object.keys(group.controls)) {
                    let ctrl = group.controls[ctrlName];
                    if (ctrl instanceof FormControl) {
                        if (!ctrl.valid && ctrl.errors) {
                            let msg = `${ctrlName}: `;
                            for (let error of Object.keys(ctrl.errors)) {
                                msg += this.getErrorMessage(ctrlName, error);
                                msg += '<br />';
                            }
                            validationMessages.push(msg);
                        }
                    }
                    if (ctrl instanceof FormGroup) {
                        let collection = ctrl as FormGroup;
                        let children = collection.controls;
                        for (let child of Object.keys(children)) {
                            let childCtrl = children[child];
                            if (childCtrl instanceof FormControl) {
                                if (!childCtrl.valid && childCtrl.errors) {
                                    let msg = `${child}: `;
                                    for (let error of Object.keys(childCtrl.errors)) {
                                        msg += this.getErrorMessage(child, error);
                                        msg += '<br />';
                                    }
                                    validationMessages.push(msg);
                                }
                            }
                        }
                    }
                }
            }
        }
        return validationMessages;
    }

    generateForm(): FormGroup {
        let f: FormGroup = new FormGroup({});
        this.fm.generateFormControlsFromModel();
        this.viewTab = 'dataset-info';
        f = this.fm.mainForm;
        return f;
    }

    save() {
        this.isLoading = true;
        this.isUploading = true;
        this.fm.sendData().subscribe(
            res => {
                this.results = res;
                if (res) {
                    if (res.status === 200) {
                        window.location.href = this.ihs.redirectToJobsRelativeUrl;
                    }
                }
            },
            function(error) {
                this.isLoading = false;
                this.isUploading = false;
                this.warnings = [];
                this.warnings.push(error);
                console.error(error); },
            function() {
                // console.log('send data complete');
            });
    }
}
