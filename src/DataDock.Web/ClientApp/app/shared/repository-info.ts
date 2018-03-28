export class RepositoryInfo {
    repositoryId: string;
    dataDockImportUrl: string;
    title: string;
    description: string;
    name: string;
    ownerLogin: string;
    ownerName: string;
    ownerHomepage: string;
    ownerAvatar: string;
    cloneUrl: string;
    apiUrl: string;
    defaultBranch: string;
    publisherInfo: any;
    language: string;
    timezoneId: string;

    constructor(options: {
        RepositoryId?: string,
        DataDockImportUrl?: string,
        Title?: string,
        Description?: string,
        Name?: string,
        OwnerLogin?: string,
        OwnerName?: string,
        OwnerHomepage?: string,
        OwnerAvatar?: string,
        CloneUrl?: string,
        ApiUrl?: string,
        DefaultBranch?: string,
        PublisherInfo?: any,
        Language?: string,
        TimezoneId?: string
    }) {

        this.repositoryId = options['RepositoryId'];
         if (!this.repositoryId) {throw new Error('`RepositoryId` is required for RepositoryInfo object'); };
        this.dataDockImportUrl = options['DataDockImportUrl'] || '';
        this.title = options['Title'] || '';
        this.description = options['Description'] || '';
        this.name = options['Name'] || '';
        this.ownerLogin = options['OwnerLogin'] || '';
        this.ownerName = options['OwnerName'] || '';
        this.ownerHomepage = options['OwnerHomepage'] || '';
        this.ownerAvatar = options['OwnerAvatar'] || '';
        this.cloneUrl = options['CloneUrl'] || '';
        this.apiUrl = options['ApiUrl'] || '';
        this.defaultBranch = options['DefaultBranch'] || '';
        this.language = options['Language'] || '';
        // console.log(this);
    }
}

