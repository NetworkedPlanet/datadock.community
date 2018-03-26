import { Injectable } from '@angular/core';
import {
  FormBuilder, FormGroup, Validators, FormArray, AbstractControl, FormControl,
  ValidatorFn
} from '@angular/forms';
import { Subject } from 'rxjs';
import { MetadataViewModel, ViewModelSection } from './metadata-viewmodel';
import { CustomValidators } from './form-field/custom-validators';
import { ImportHelperService } from './import-helper.service';
import { ApiService } from './api.service';

/*

The FormManager class handles the communication between the services that
 build the form controls (or sets of form controls in form groups), and the components
  that interact with the user.
The resulting group of form controls is stored in the mainForm variable
so that it is accessible from multiple components

 */

@Injectable()
export class FormManager {

  private warningAddedSource = new Subject<string>();
  warningAdded$ = this.warningAddedSource.asObservable();

  private errorAddedSource = new Subject<string>();
  errorAdded$ = this.errorAddedSource.asObservable();

  private toggleFieldCollectionSource = new Subject<string>();
  toggleFieldCollection$ = this.toggleFieldCollectionSource.asObservable();

  // this is the view model defining the set of fields that are to be shown in the form
  public metadataViewModel: MetadataViewModel;

  // this is the set of controls that all metadata components display and update
  public mainForm: FormGroup;

  public showOnHomePage: boolean;
  public saveAsSchema: boolean;

  constructor(
      private importAppHelper: ImportHelperService,
      private fb: FormBuilder,
      private api: ApiService) {
    // initialise
    this.mainForm = fb.group({});
    this.showOnHomePage = true;
    this.saveAsSchema = false;
  }

  addWarning(message: string) {
    this.warningAddedSource.next(message);
  }

  addError(message: string) {
    this.errorAddedSource.next(message);
  }

  /*
  If a column is excluded (suppressInOutput) then disable the
  entire field collection so that other properties of that
  column cannot be modified
   */
  toggleFieldCollection(name: string, suppress: boolean) {
    let colDefTab = this.getCollectionControls('cols_basic', name);
    let advTab = this.getCollectionControls('cols_advanced', name);
    let controlsToToggle = colDefTab.concat(advTab);
    for (let ctrlName of controlsToToggle) {
      if (ctrlName.indexOf('_suppress') < 0) {
        try {
          let field = this.getField(ctrlName);
          if (field) {
            let ctrl = field[1];
            if (suppress) {
              ctrl.disable();
            } else {
              ctrl.enable();
            }
          }
        } catch (e) {
          console.error(e);
        }
      }
    }
    this.toggleFieldCollectionSource.next(name);
  }

  isFieldCollectionSuppressed(collectionName: string): boolean {
    if (collectionName) {
      let field = this.getField(collectionName + '_suppress');
      if (field && field[0]) {
        let suppressed = field[0]['defaultValue'];
        if (suppressed) {
          return true;
        }
      }
    }
    return false;
  }

  setHomePageDisplay(showOnHomePage: boolean): void {
    this.showOnHomePage = showOnHomePage;
  }

  setSaveTemplateDisplay(saveAsSchema: boolean): void {
    this.saveAsSchema = saveAsSchema;
  }

  getColumnTitle(columnName: string): string {
    let title = '';
    for (let c of this.importAppHelper.csvFile.columnSet) {
      if (c.name === columnName) {
        title = c.title;
      }
    }
    return title;
  }

  /*
   buildForm() retrieves the metadata view model from the form-field.service and builds a form for
   components to interact with
   It should only be called once from the parent component after a file has been selected
   */
  generateFormControlsFromModel(): void {
    if (IN_DEBUG) {
      console.log('Generating form controls from view model...', this.metadataViewModel);
    }

    try {
      // for each section in the view model, build the form fields and groups
      let sections = {};
      for (let section of this.metadataViewModel.sections) {
        let fields = {};
        for (let field of section.fields) {
          let fieldValidators = this.getFieldValidators(field);
          fields[field.name] = [field.defaultValue, fieldValidators];
        }
        let fieldsGroup = this.fb.group(fields);

        let collections = {};
        for (let fc of section.fieldCollections) {
          if (fc.name === '' && fc.columnNumber > 0) {
            let warning = `Column ${fc.columnNumber} has no heading and therefore cannot be included in the outputted data`;
            this.addWarning(warning);
          }
          let fgColumnFields = {};
          for (let field of fc.fields) {
            let fieldValidators = this.getFieldValidators(field);
            if (fc.disabled) {
              // we are in an auto-suppressed column
              let suppressedCtrl: any;
              if (field.name.indexOf('_suppress') >= 0) {
                // tick the suppress checkbox
                suppressedCtrl = new FormControl(true, fieldValidators);
              } else {
                suppressedCtrl = new FormControl(field.defaultValue, fieldValidators);
              }
              suppressedCtrl.disable();
              fgColumnFields[field.name] = suppressedCtrl;
            } else {
              let ctrl = new FormControl(field.defaultValue, fieldValidators);
              fgColumnFields[field.name] = ctrl;
            }
            // let control = new FormControl({value: field.defaultValue, disabled: field.disabled}, fieldValidators);
            // fgColumnFields[field.name] = control;
          }
          collections[fc.name] = this.fb.group(fgColumnFields);
        }
        let collectionsGroup = this.fb.group(collections);

        let arr = this.fb.array([fieldsGroup, collectionsGroup]);
        sections[section.name] = arr;
      }

      this.mainForm = this.fb.group(sections);
      if (IN_DEBUG) {
        console.log('the form', this.mainForm);
      }

      // this.displayForm = true;
    } catch (e) {
      // console.log((<Error>e).message);
      this.addError((<Error>e).message);
    }
  }

  private getFieldValidators(field): ValidatorFn {
    let result = [];

    for (let v of field.validations) {
      result.push((v.data ? Validators[v.type](v.data) : Validators[v.type]));
    }
    for (let cv of field.customValidations) {
      result.push((cv.data ? CustomValidators[cv.type](cv.data) : CustomValidators[cv.type]));
    }
    return (result.length > 0) ? Validators.compose(result) : null;
  }

  getViewModelTableHeaders(sectionName: string): string[] {
    if (this.metadataViewModel) {
      let section = this.metadataViewModel.sections.filter(item => item.name === sectionName)[0];
      if (section) {
        return section.tableHeaders;
      } else {
        console.error(`Unable to find tableHeaders in section ${sectionName}`);
        return [];
      }
    }
  }

  getSection(sectionName: string): ViewModelSection {
    if (this.metadataViewModel) {
      let section = this.metadataViewModel.sections.filter(item => item.name === sectionName)[0];
      return section;
    }
  }

  getColumnsList(section: string): Array<any> {
    let s = <FormGroup> this.mainForm.controls[section];
    let fieldCollectionGroup = s.controls[1];
    return Object.keys(fieldCollectionGroup['controls']);
  }

  getCollectionControls(sectionName: string, columnName: string): Array<any> {
    // console.log('getCollectionControls section: ' + sectionName + ' columnName: ' + columnName );
    let s: FormGroup = <FormGroup> this.mainForm.controls[sectionName];
    let fieldCollectionGroup: FormArray = <FormArray> s.controls[1];
    let c: FormGroup = <FormGroup> fieldCollectionGroup.controls[columnName];
    // console.log(c['controls']);
    return Object.keys(c['controls']);
  }

  getControls(sectionName: string): Array<any> {
    // console.log('getControls:', this.mainForm, sectionName);
    if (this.mainForm && sectionName) {
      let section = <FormArray> this.mainForm.controls[sectionName];
      if (section) {
        let formGroup = section.controls[0];
        return Object.keys(formGroup['controls']);
      } else {
        // console.log('Cannot get controls as the section cannot be found');
      }
    } else {
      // console.log('Cannot get controls as the form or sectionName is not supplied');
    }
    return [];
  }

  getField(fieldName: string): Array<any> {
    // console.log('getField:', this.mainForm, fieldName);
    if (this.mainForm && fieldName) {
      let result = [];
      let fieldFound: any = {};
      let controlFound: any;
      let sectionFoundIn = '';
      let isInsideCollection: boolean;

      // search the view model for the named field
      if (this.metadataViewModel && this.metadataViewModel.sections) {
        this.metadataViewModel.sections.forEach(section => {
          // first search section's root fields
          section.fields.forEach(field => {
            if (field.name === fieldName) {
              fieldFound = field;
              result.push(field);
              sectionFoundIn = section.name;
              isInsideCollection = false;
            }
          });
          // then search section's fields inside field collections
          section.fieldCollections.forEach(fc => {
            fc.fields.forEach(field => {
              if (field.name === fieldName) {
                fieldFound = field;
                result.push(field);
                sectionFoundIn = section.name;
                isInsideCollection = true;
              }
            });
          });
        });

        if (result.length <= 0) {
          this.addError(`Field with name: ${fieldName} not found`);
          return [];
        }

        // search the form for the control to go with the field, and push it to the result
        let formSection: FormArray = <FormArray> this.mainForm.controls[sectionFoundIn];
        let formGroup: FormGroup;
        if (isInsideCollection) {
          let fieldCollectionGroup = <FormGroup> formSection.controls[1];
          formGroup = <FormGroup> fieldCollectionGroup.controls[fieldFound.collectionName];
        } else {
          formGroup = <FormGroup> formSection.controls[0];
        }
        if (formGroup) {
          let control: FormGroup = <FormGroup> (formGroup.controls[fieldFound.name]);
          result.push(control);
        }

        //  console.log('returning search results');
        //  console.log(search);
        // TODO format check to make sure we are returning a field and a control
        return result;

      } else {
        return [];
      }
    }
  }

  /*
   Publish button, generates metadata.json from the form and sends
   it and the CSV file to the DataDock Data API
   */
  sendData() {
    if (IN_DEBUG) {
      console.log('sendData()');
    }
    let metadata = this.generateMetadataJsonFromFormValues();
    if (IN_DEBUG) {
      console.log('metadata', metadata);
    }
    let formData = new FormData();
    // console.log('appending data to formData ' + this.csvFile.filename);
    // console.log(this.csvFile);
    formData.append('file', this.importAppHelper.csvFile.file, this.importAppHelper.csvFile.filename);
    formData.append('metadata', JSON.stringify(metadata));
    if (IN_DEBUG) {
      console.log('appending target repo to formData', this.importAppHelper.targetRepository);
    }
    formData.append('targetRepository', JSON.stringify(this.importAppHelper.targetRepository));
    formData.append('showOnHomePage', JSON.stringify(this.showOnHomePage));
    formData.append('saveAsSchema', JSON.stringify(this.saveAsSchema));
    if (IN_DEBUG) {
      console.log('formData', formData);
      console.log('sending to API');
    }
    return this.api.post(formData);
  }

  /*
   Returns plain JSON of the metadata
   */
  private generateMetadataJsonFromFormValues(): any {
    let metadata = {};
    // base metadata info
    metadata['@context'] = 'http://www.w3.org/ns/csvw';
    metadata['url'] = this.importAppHelper.getCsvFileUri();

    let baseSection = this.mainForm.value['base'];
    if (baseSection && baseSection instanceof Array) {
      let baseSectionFields = baseSection[0];
      metadata['dc:title'] = baseSectionFields.title;
      metadata['dc:description'] = baseSectionFields.description;
      metadata['dcat:keyword'] = baseSectionFields.keywords;
      metadata['dc:license'] = baseSectionFields.license;
    }

    // columns
    let tableSchemaColumns = [];

    let formSectionColumns = this.mainForm.value['cols_basic'];
    let columnsFieldCollections = {};
    let formSectionAdvanced = this.mainForm.value['cols_advanced'];
    let advancedFieldCollections = {};

    if (formSectionColumns && formSectionColumns instanceof Array) {
      columnsFieldCollections = formSectionColumns[1];
    }
    if (formSectionAdvanced && formSectionAdvanced instanceof Array) {
      let advancedFields = formSectionAdvanced[0];
      advancedFieldCollections = formSectionAdvanced[1];
      metadata['aboutUrl'] = advancedFields['identifier'];
    }

    if (this.importAppHelper.csvFile.columnSet.length < 1) {
      // todo - add a warning
    }

    let cols = this.importAppHelper.csvFile.columnSet;
    for (let i = 0; i < cols.length; i++) {
      let c = cols[i];
      if (c) {
        try {
          let columnName = c.name;

          let pnTitle = `${columnName}_title`;
          let pnPropertyUrl = `${columnName}_property_url`;
          let pnDatatype = `${columnName}_datatype`;
          let pnSuppress = `${columnName}_suppress`;

          let metadataColumn = {};
          metadataColumn['name'] = columnName;

          let basicFields = columnsFieldCollections[columnName];
          let advancedFields = advancedFieldCollections[columnName];

          if (basicFields) {
            let suppress = basicFields[pnSuppress];
            if (suppress) {
              metadataColumn['suppressOutput'] = true;
            } else {
              metadataColumn['titles'] = [basicFields[pnTitle]];
              if (basicFields[pnDatatype] === 'uri') {
                metadataColumn['valueUrl'] = `{${columnName}}`;
              } else {
                metadataColumn['datatype'] = basicFields[pnDatatype];
                if (advancedFields) {
                  metadataColumn['propertyUrl'] = advancedFields[pnPropertyUrl];
                }
              }
            }
            tableSchemaColumns.push(metadataColumn);
          }
        } catch (ex) {
          console.error(ex);
        }
      }
    }
    if (IN_DEBUG) {
      console.log('tableSchemaColumns', tableSchemaColumns);
    }
    let ts = {};
    ts['columns'] = tableSchemaColumns;
    metadata['tableSchema'] = ts;
    // console.log(metadata);
    return metadata;
  }
}
