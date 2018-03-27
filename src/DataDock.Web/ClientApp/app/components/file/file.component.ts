import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { PapaParseService } from 'ngx-papaparse';

import { FormManager, CsvFile } from '../../shared';
import { ViewModelHelperService, SchemaHelperService, SchemaService } from '../../shared';
import { Response } from "@angular/http";
import { AppService } from '../../shared/app.service';

@Component({
  selector: 'dd-file',
  templateUrl: './file.component.html',
  styleUrls: ['./file.component.css']
})
export class FileComponent implements OnInit, OnDestroy  {

  MAX_FILE_SIZE = 4194304;
  filename: string;
  error: string;
  isLoading = false;

  showTemplateErrorButtons = false;

  repoUriCheck = false;
  ownerId: string;
  repoId: string;
  schemaId: string;
  uploadText: string;

  private sub: any;
  restartLink: string;

  constructor(
      private router: Router,
      private route: ActivatedRoute,
      private papa: PapaParseService,
      private appService: AppService,
      private ss: SchemaService,
      private shs: SchemaHelperService,
      private vmhs: ViewModelHelperService,
      public fm: FormManager
      ) {
    this.uploadText = 'Select CSV file';
  }

  ngOnInit() {
    this.isLoading = false;
    this.filename = '';
    this.error = '';

      this.sub = this.route.params.subscribe(params => {
          this.ownerId = params['ownerId'];
          this.repoId = params['repoId'];
          this.schemaId = params['schemaId'];
          // todo get schema title from model
      });
  }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }

  fileChangeEvent(fileInput: any) {
    this.isLoading = true;

    if (fileInput.target.files && fileInput.target.files[0]) {
      let file = fileInput.target.files[0];
      this.filename = file.name;
      if (IN_DEBUG) {
        console.log(this.filename);
      }
      if (file.size > this.MAX_FILE_SIZE) {
        this.error = 'File size is over the 4MB limit. Reduce file size before trying again.';
        this.isLoading = false;
      } else {
        let config = this.buildConfig();
        this.papa.parse(file, config); // see parseComplete()
      }
    }
  }

  buildConfig() {
    return {
      complete: this.parseComplete.bind(this),
      error: this.parseError.bind(this)};
  }

  parseComplete(results: any, file: any): void {
    if (IN_DEBUG) {
      console.log('Parsing complete:', results, file);
    }

    let csvFile: CsvFile = new CsvFile();
    csvFile.initialise(results, file);
    this.appService.setSource(csvFile, this.ownerId, this.repoId, this.schemaId);
    this.processFileInfoViewModelAndRedirect();
  }

  processFileInfoViewModelAndRedirect() {
    try {
      let csvFile = this.appService.csvFile;
      let dataSlice = csvFile.getDataSlice();

      if (this.ownerId && this.schemaId) {
        // get the schema via API
        this.ss.get(this.ownerId, this.schemaId).subscribe(
            apiResponse => {
              // successful apiResponse =  obj {status: 200, schema: {} }
              if (IN_DEBUG) {
                console.log('schema api response', apiResponse);
              }
              if (apiResponse && apiResponse.status === 200) {
                // setting schema (can be read from the metadata view model builder?
                this.shs.setSchema(this.ownerId, this.schemaId, apiResponse.schemaInfo);
              } else {
                this.error = `Unable to retrieve template for use in import. Do you want to:`;
                this.showTemplateErrorButtons = true;
              }
            },
            error => {
              let res = <Response>error;
              let statusText = '';
              if (res.status) {
                statusText = res.statusText ? ` ( res.statusText )` : '';
              }
              this.error = `Unable to retrieve template for use in import. Do you want to:`;
              this.showTemplateErrorButtons = true;
              if (IN_DEBUG) {
                console.error('API error', res);
              }
            },
            () => {
              if (IN_DEBUG) {
                console.log('subscription complete');
              }
              // when schema returned, build view model
              this.buildAndRedirect(csvFile, dataSlice);
            });
      } else {
        // build without schema
        this.continue();
      }
    } catch (e) {
      this.error = `Error retrieving template for use in import . Do you wish to 
<a href="#" (click)="continue()">continue without the template</a> or 
<a href="/${this.ownerId}/library">return to check the template library</a>?`;
      if (IN_DEBUG) {
        console.error('error hit', (<Error>e));
      }
    }
  }

  backToLibrary() {
    let libUrl = `/${this.ownerId}/library`;
    //  window.location.href = libUrl;
  }

  continue() {
    // build without schema
    let csvFile = this.appService.csvFile;
    let dataSlice = csvFile.getDataSlice();
    this.shs.hasSchema = false;
    this.buildAndRedirect(csvFile, dataSlice);
  }

  buildAndRedirect(csvFile: CsvFile, dataSlice: Array<any>) {
    let viewModel = this.vmhs.buildMetadataViewModel(this.appService.prefix, csvFile.filename, csvFile.columnSet, dataSlice);
    if (IN_DEBUG) {
      console.log('view model building complete', viewModel);
    }
    this.fm.metadataViewModel = viewModel;
    let mdUrl = `/${this.ownerId}/${this.repoId}/define`;
    this.router.navigate([mdUrl]);
  }

  // TODO
  parseError(err: any, file: any): void {
    console.error('ERROR:', err, file);
  }


}
