import { CsvFile } from '../../shared/csv-file';
import { RepositoryInfo } from '../../shared/repository-info';
import { DashboardModel } from '../../shared/dashboard-model';

export class MockAppService {

    DATADOCK_URL = 'http://datadock.io/';
    public dashboardViewModel: DashboardModel;
    public targetRepository: RepositoryInfo;

    public ownerId: string;
    public repoId: string;
    public schemaId: string;

    public prefix: string;

    public csvFile: CsvFile;

    public restartImportRelativeUrl: string;
    public redirectToJobsRelativeUrl: string;

    setSource(file: CsvFile, ownerId: string, repoId: string, schemaId: string): void {
        console.warn('MockImportHelperService.setSource()');
        // repeat the logic of the real FormManager here so that stubs can be set from tests
        this.csvFile = file;
        this.ownerId = ownerId;
        this.repoId = repoId;
        this.schemaId = schemaId;
    }

    setTargetRepository(targetRepository: RepositoryInfo): void {
        console.warn('MockImportHelperService.setTargetRepository()');
        this.prefix = `${this.DATADOCK_URL}${targetRepository.repositoryId}/`;
        this.targetRepository = targetRepository;
        this.restartImportRelativeUrl = `/${targetRepository.repositoryId}/import`;
        this.redirectToJobsRelativeUrl = `/${targetRepository.repositoryId}/jobs`;
    }

    public setDashboardModel(dashModel: DashboardModel): void {
        console.warn('MockImportHelperService.setDashboardModel()');
        this.dashboardViewModel = dashModel;
    }

}