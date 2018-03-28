import { Injectable } from '@angular/core';
import { Tooltip } from './form-field/tooltip';
import { RequiredValidator, PatternValidator, MinLengthValidator } from './form-field/validator';
import { TextFormField, HiddenFormField, SelectFormField, RadioFormField,
  NumberFormField, CheckboxFormField, TextAreaFormField, TagsFormField } from './form-field/form-field';
import { OPTIONS_DATATYPES } from './form-field/options-datatypes';
import { MetadataViewModel, ViewModelSection, FieldCollection } from './metadata-viewmodel';
import { DatatypeService } from './datatype.service';
import { OPTIONS_SUPPRESS } from './form-field/options-suppress';
import { NoSpaceValidator } from './form-field/custom-validator';
import { SchemaHelperService } from './schema-helper.service';
import { Globals } from '../globals';

@Injectable()
export class ViewModelHelperService {

  // an array of the column set (index, name and title)
  private columnSet = [];

  // the top 10 rows of the raw data (not including header row)
  private dataSampleRows: Array<any>;

  identifierOptions = [];


  constructor(private globals: Globals, private ds: DatatypeService, private shs: SchemaHelperService) {}

  /*
    getMetadataViewModel returns a MetadataViewModel object that describes the shape of the form,
    based on the columns of the supplied CSV file.
    The MetadataViewModel object is split into sections containing fields (e.g. the base section contains
    fields such as title, description, license and tags; the cols_basic section contains the datatypes drop-down lists
    for each of the columns; the advanced section contains the input fields for the property URLs for each of the
    columns)
   */
  buildMetadataViewModel(prefix: string, filename: string, columnSet: Array<any>, rowDataSample: Array<any>): MetadataViewModel {

    let schema = this.shs.getSchema();
    if (this.globals.config.inDebug) {
      console.log('building view model with supplied values: ');
      console.log('prefix', prefix);
      console.log('filename', filename);
      console.log('columnSet', columnSet);
      console.log('rowDataSample', rowDataSample);
      console.log('schema', schema);
    }

    this.columnSet = columnSet;
    this.dataSampleRows = rowDataSample;

    let mvm = new MetadataViewModel();

    let s1 = new ViewModelSection();
    s1.name = 'base';
    s1.displayType = 'base';
    s1.fields = this.getBaseFields(filename);

    mvm.sections.push(s1);

    let s2 = new ViewModelSection();
    s2.name = 'cols_basic';
    s2.displayType = 'columns';
    s2.fieldCollections = this.getColumnFieldsBasic();
    s2.tableHeaders = ['Title', 'Datatype', 'Suppress In Output'];

    mvm.sections.push(s2);

    let s3 = new ViewModelSection();
    s3.name = 'cols_advanced';
    s3.displayType = 'advanced';
    s3.tableHeaders = ['Property (URL)'];



    // about url options
    let resourcePrefix = prefix + 'id/resource/';
    let defaultAboutUrlPrefix = resourcePrefix + encodeURIComponent(filename);

    let templateIdentifier = this.shs.getMetadataIdentifier(defaultAboutUrlPrefix, '');
    if (this.globals.config.inDebug) {
      console.log('template identifier', templateIdentifier);
    }

    let identifier = this.buildIdentifierOptions(defaultAboutUrlPrefix, filename, templateIdentifier);

    let identifierSelector = new SelectFormField({
      name: 'identifier',
      label: 'Identifier',
      tooltip: new Tooltip('Change the base identifier'),
      showLabel: true,
      validations: [
        new RequiredValidator('Required.'),
      ],
      options: this.identifierOptions,
      defaultValue: identifier,
      info: '<a href="http://networkedplanet.com/datadock/user-guide/selecting-an-identifier.html" target="_blank" ' +
      'title="More info about identifiers">More info about identifiers (opens in new window)</a>'
    });
    s3.fields.push(identifierSelector);
    s3.fieldCollections = this.getCsvColumnsFieldsAdvanced(prefix);

    mvm.sections.push(s3);

    return mvm;
  }

  private buildIdentifierOptions(aboutUrlPrefix: string, filename: string, templateAboutUrl: string): string {

    let templateSetError: boolean;
    let found = false;

    let defaultAboutUrl =  aboutUrlPrefix + '/row_{_row}';
    let d = { display: 'Row Number', value: defaultAboutUrl};
    this.identifierOptions.push(d);

    if (templateAboutUrl && templateAboutUrl === defaultAboutUrl) {
      found = true;
    }

    if (this.columnSet.length > 0) {
        this.columnSet.forEach(colInfo => {
          if (colInfo.name !== '') {
            let aboutUrl = aboutUrlPrefix + '/' + colInfo.name + '/{' + colInfo.name + '}';
            let displayText = colInfo.title;
            let option = { display: displayText, value: aboutUrl};
            if (templateAboutUrl && templateAboutUrl === aboutUrl) {
              found = true;
              // set in HTML
              option['selected'] = true;
            }
            this.identifierOptions.push(option);
          }
        });
    }

    if (templateAboutUrl && !found) {
      // template aboutUrl supplied but no matching aboutUrls found.
      return defaultAboutUrl;
    }
    if (templateAboutUrl && found) {
      return templateAboutUrl;
    }
    return defaultAboutUrl;
  }

  private getBaseFields(filename: string) {
    let fields = [
      new TextFormField({
        name: 'title',
        label: 'Dataset Title',
        validations: [
          new RequiredValidator('Required.'),
          new MinLengthValidator(5, 'Title should be at least 5 characters.'),
        ],
        defaultValue: this.shs.getMetadataTitle() ? this.shs.getMetadataTitle() : filename,
        tooltip: new Tooltip('Please enter your name'),
        showLabel: true
      }),
      new TextAreaFormField({
        name: 'description',
        label: 'Description',
        showLabel: true,
        defaultValue: this.shs.getMetadataDescription()
      }),
      new SelectFormField({
        name: 'license',
        label: 'License',
        tooltip: new Tooltip('Please choose your dataset\'s license.'),
        validations: [
          new RequiredValidator('Required.'),
        ],
        showLabel: true,
        defaultValue: this.shs.getMetadataLicenseUri(),
        info: '<a href="http://networkedplanet.com/datadock/user-guide/choosing-a-license.html" target="_blank" ' +
        'title="More info about licenses">More info about licenses (opens in new window)</a>'
      }),
      new TagsFormField({
        name: 'keywords',
        label: 'Tags',
        placeholder: 'Enter a keyword and press enter',
        showLabel: true,
        defaultValue: this.shs.getMetadataTags()
      })
    ];

    return fields;
  }


  private getColumnFieldsBasic() {

    let colDataSample = this.getRawDataByColumn();
    if (this.globals.config.inDebug) {
      console.log('getColumnFieldsBasic', this.dataSampleRows, colDataSample);
    }

    let fcs: FieldCollection[] = [];
    if (this.columnSet) {

      for (let column of this.columnSet) {

        // template from schema
        let template = this.shs.getMetadataColumnTemplate(column.name);

        let templateDatatype = this.shs.getColumnDatatype(template);
        let colDatatype = templateDatatype ? templateDatatype :
            this.guessDatatype(column.index, column.name, colDataSample);

        // create fields
        let fc = new FieldCollection();
        fc.index = column.index;
        fc.name = column.name;
        fc.disabled = column.name === '';
        fc.columnNumber = column.index + 1;
        fc.fields = [
          new TextFormField({
            name: column.name + '_title',
            label: 'Title',
            defaultValue: this.shs.getColumnTitle(template, column.title),
            validations: [
              new RequiredValidator('Required.')
            ],
            customValidations: [
            ],
            tooltip: new Tooltip('The title for the column'),
            showLabel: false,
            collectionName: column.name,
            disabled: column.name === ''
          }),
          new SelectFormField({
            name: column.name + '_datatype',
            label: 'Datatype',
            options: OPTIONS_DATATYPES,
            defaultValue: colDatatype,
            tooltip: new Tooltip('What type of data is in this column?'),
            showLabel: false,
            collectionName: column.name,
            disabled: column.name === ''
          }),
          new CheckboxFormField({
            name: column.name + '_suppress',
            label: 'Suppress this column in the output data?',
            options: OPTIONS_SUPPRESS,
            showLabel: false,
            defaultValue: this.shs.getColumnSuppressed(template),
            collectionName: column.name,
            checked: column.name === '',
            disabled: column.name === ''
          })
        ];

        fcs.push(fc);
        // console.log(fc);
      }

    } else {
      // log error
    }
    return fcs;
  }

  private guessDatatype(colIndex: number, colName: string, colDataSample: Array<any>): string {
    let sniff: any;
    if (colDataSample) {
        let colData = colDataSample[colIndex];
        sniff = this.ds.detectDataType(colData, colName);
        // console.log(sniff);
    }
    return sniff.datatype;
  }


  /*
   the sample is supplied as an array of 10 rows of raw data (excluding header row).
   To guess the datatype of each column, we need to organise this in arrays that hold
   data from the cells of each *column*
   */
  private getRawDataByColumn(): Array<Array<string>> {
    let colDataSample: Array<any> = [];
    if (this.dataSampleRows) {
      // get number of columns
      let r1 = this.dataSampleRows[0];
      if (r1) {
        if (this.globals.config.inDebug) {
         //  console.log('Processing row ', r1);
        }
        let numCols = r1.length;
        for (let i = 0; i < numCols; i++) {
          let columnData: Array<string> = [];
          this.dataSampleRows.forEach(row => {
            let cellData = row[i];
            if (cellData) {
              columnData.push(cellData);
            }
          });
          if (this.globals.config.inDebug) {
            // console.log('Produced ', columnData);
          }
          colDataSample.push(columnData);
        }
      }
    }
    return colDataSample;
  }

  private getCsvColumnsFieldsAdvanced(prefix: string) {
    let fcs: FieldCollection[] = [];

    let idx = 0;
    for (let column of this.columnSet) {

      // template from schema
      let template = this.shs.getMetadataColumnTemplate(column.name);

      // build fields
      let fc = new FieldCollection();
      fc.index = idx;
      fc.name = column.name;
      fc.disabled = column.name === '';
      fc.fields = [
        new TextFormField({
          name: column.name + '_property_url',
          defaultValue: this.shs.getColumnPropertyUrl(template, column.name !== '' ? `${prefix}id/definition/${column.name}` : ''),
          label: column.title + ' Property (URL)',
          showLabel: false,
          collectionName: column.name,
          disabled: column.name === '',
          allowReset: true,
          validations: [
            new RequiredValidator('Required.'),
            new PatternValidator('^https?(?:(\://))\\S+[^/#]$',
                'Must be a URL that does not end with a hash or slash.')
          ]})
      ];
      fcs.push(fc);
      idx++;
    }
    return fcs;
  }


}
