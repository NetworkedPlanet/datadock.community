import { Injectable } from '@angular/core';
import { DashboardModel } from './dashboard-model';
import { RepositoryInfo } from './repository-info';
import { CsvFile } from './csv-file';
import { Globals } from '../globals';

/*
A helper service for data sharing across the whole import
application's components and services
 */
@Injectable()
export class AppService {

    DATADOCK_URL = 'http://datadock.io/';

    public ownerId: string;
    public repoId: string;
    public schemaId: string;

    public prefix: string;

    public csvFile: CsvFile;

    public restartImportRelativeUrl: string;
    public redirectToJobsRelativeUrl: string;

    constructor(private globals: Globals) {
        this.ownerId = '';
        this.repoId = '';
        this.schemaId = '';
    }

    public initialise(file: CsvFile, ownerId: string, repoId: string, schemaId: string): void {
        if (this.globals.config.inDebug) {
            console.log('initialising AppService', file, ownerId, repoId, schemaId);
        }
        this.csvFile = file;
        this.ownerId = ownerId;
        this.repoId = repoId;
        this.schemaId = schemaId;
        if (repoId) {
            this.prefix = `${this.DATADOCK_URL}${ownerId}/${repoId}/`;
            this.restartImportRelativeUrl = `/${ownerId}/${repoId}/import`;
            this.redirectToJobsRelativeUrl = `/${ownerId}/${repoId}/jobs`;
        }
    }

    public getCsvFileUri() {
        return  `${this.prefix}id/dataset/${this.csvFile.filename}`;
    }
}
