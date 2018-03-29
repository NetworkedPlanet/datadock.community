using System;
using System.IO;
using Datadock.Common.Models;

namespace DataDock.Worker
{
    
    public class DataDockRepositoryFactory : IDataDockRepositoryFactory
    {
        private readonly WorkerConfiguration _config;
        private readonly IQuinceStoreFactory _quinceStoreFactory;
        private readonly IHtmlGeneratorFactory _htmlGeneratorFactory;

        public DataDockRepositoryFactory(WorkerConfiguration config, 
            IQuinceStoreFactory quinceStoreFactory, 
            IHtmlGeneratorFactory htmlGeneratorFactory)
        {
            _config = config;
            _quinceStoreFactory = quinceStoreFactory;
            _htmlGeneratorFactory = htmlGeneratorFactory;
        }

        public IDataDockRepository GetRepositoryForJob(JobInfo jobInfo, IProgressLog progressLog)
        {
            var repoPath = Path.Combine(_config.RepoBaseDir, jobInfo.JobId);
            
            // TODO: These should not be hard-coded
            var baseIri = new Uri($"http://datadock.io/{jobInfo.OwnerId}/{jobInfo.RepositoryId}/");
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
                _htmlGeneratorFactory,
                rdfResourceFileMapper,
                htmlResourceFileMapper);
        }
    }
}
