import { Injectable } from '@angular/core';
import { DashboardModel } from './dashboard-model';
import { RepositoryInfo } from './repository-info';
import { CsvFile } from './csv-file';

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

    constructor() {
        this.ownerId = '';
        this.repoId = '';
        this.schemaId = '';
    }

    public setSource(file: CsvFile, ownerId: string, repoId: string, schemaId: string): void {
        if (IN_DEBUG) {
            console.log('setting source on ImportHelperService', file, ownerId, repoId, schemaId);
        }
        this.csvFile = file;
        this.ownerId = ownerId;
        this.repoId = repoId;
        this.schemaId = schemaId;
    }

    public getCsvFileUri() {
        return  `${this.prefix}id/dataset/${this.csvFile.filename}`;
    }
}