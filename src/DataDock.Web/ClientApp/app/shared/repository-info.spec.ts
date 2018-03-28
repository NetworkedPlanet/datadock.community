import { RepositoryInfo } from './repository-info';
describe('RepositoryInfo: ', () => {

    it('should create an instance', () => {
        const repoInfo = new RepositoryInfo(
            {
                'RepositoryId': 'jennet/legendary-octo-spork',
                'DataDockImportUrl': '/jennet/legendary-octo-spork/import',
                'Title': null,
                'Description': 'Tester repo',
                'Name': 'legendary-octo-spork',
                'OwnerLogin': 'jennet',
                'OwnerName': null,
                'OwnerHomepage': 'https://github.com/jennet',
                'OwnerAvatar': 'https://avatars2.githubusercontent.com/u/4940135?v=3',
                'CloneUrl': 'https://github.com/jennet/legendary-octo-spork.git',
                'ApiUrl': 'https://api.github.com/repos/jennet/legendary-octo-spork',
                'DefaultBranch': 'master',
                'PublisherInfo': null,
                'Language': null,
                'TimezoneId': null
            });
        expect(repoInfo).toBeTruthy();
        expect(repoInfo.repositoryId).toEqual('jennet/legendary-octo-spork');
        expect(repoInfo.dataDockImportUrl).toEqual('/jennet/legendary-octo-spork/import');
        expect(repoInfo.title).toEqual('');
        expect(repoInfo.description).toEqual('Tester repo');
        expect(repoInfo.name).toEqual('legendary-octo-spork');
        expect(repoInfo.ownerLogin).toEqual('jennet');
        expect(repoInfo.ownerName).toEqual('');
        expect(repoInfo.ownerHomepage).toEqual('https://github.com/jennet');
        expect(repoInfo.ownerAvatar).toEqual('https://avatars2.githubusercontent.com/u/4940135?v=3');
        expect(repoInfo.cloneUrl).toEqual('https://github.com/jennet/legendary-octo-spork.git');
        expect(repoInfo.apiUrl).toEqual('https://api.github.com/repos/jennet/legendary-octo-spork');
        expect(repoInfo.defaultBranch).toEqual('master');
        expect(repoInfo.publisherInfo).toBeUndefined();
        expect(repoInfo.language).toEqual('');
        expect(repoInfo.timezoneId).toBeUndefined();
    });

});
