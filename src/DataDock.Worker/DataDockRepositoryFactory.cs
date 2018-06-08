using System;
using System.IO;
using DataDock.Common.Models;

namespace DataDock.Worker
{
    
    public class DataDockRepositoryFactory : IDataDockRepositoryFactory
    {
        private readonly WorkerConfiguration _config;
        private readonly IQuinceStoreFactory _quinceStoreFactory;
        private readonly IFileGeneratorFactory _fileGeneratorFactory;

        public DataDockRepositoryFactory(WorkerConfiguration config, 
            IQuinceStoreFactory quinceStoreFactory, 
            IFileGeneratorFactory fileGeneratorFactory)
        {
            _config = config;
            _quinceStoreFactory = quinceStoreFactory;
            _fileGeneratorFactory = fileGeneratorFactory;
        }

        public IDataDockRepository GetRepositoryForJob(JobInfo jobInfo, IProgressLog progressLog)
        {
            var repoPath = Path.Combine(_config.RepoBaseDir, jobInfo.JobId);
            
            var baseIri = new Uri($"{_config.BaseUrl}/{jobInfo.OwnerId}/{jobInfo.RepositoryId}/");
            var resourceBaseIri = new Uri(baseIri, "id/");
            var rdfResourceFileMapper = new ResourceFileMapper(
                new ResourceMapEntry(resourceBaseIri, Path.Combine(repoPath, "data")));
            var htmlResourceFileMapper = new ResourceFileMapper(
                new ResourceMapEntry(resourceBaseIri, Path.Combine(repoPath, "page")));

            return new DataDockRepository(
                repoPath,
                baseIri,
                progressLog,
                _quinceStoreFactory,
                _fileGeneratorFactory,
                rdfResourceFileMapper,
                htmlResourceFileMapper);
        }
    }
}
