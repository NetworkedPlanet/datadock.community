using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Moq;

namespace DataDock.Worker.Tests
{

    public class BaseDataDockRepositorySpec : IDisposable
    {
        private readonly DirectoryInfo _testDir;
        protected string RepoPath;
        protected readonly MockQuinceStore QuinceStore;
        protected readonly Uri BaseUri;
        protected readonly DataDockRepository Repo;

        public BaseDataDockRepositorySpec()
        {
            var runId = DateTime.UtcNow.Ticks.ToString();
            _testDir = Directory.CreateDirectory(runId);
            RepoPath = _testDir.FullName;

            QuinceStore = new MockQuinceStore();
            var quinceStoreFactory = new Mock<IQuinceStoreFactory>();
            quinceStoreFactory.Setup(x => x.MakeQuinceStore(RepoPath)).Returns(QuinceStore);
            var htmlGeneratorFactory = new Mock<IHtmlGeneratorFactory>();
            BaseUri = new Uri("http://datadock.io/test/repo/");
            var rdfResourceMapper = new ResourceFileMapper(
                new ResourceMapEntry(new Uri(BaseUri, "id"), "data"));
            var htmlResourceMapper = new ResourceFileMapper(
                new ResourceMapEntry(new Uri(BaseUri, "id"), "doc"));

            Repo = new DataDockRepository(RepoPath, BaseUri, new MockProgressLog(),
                quinceStoreFactory.Object, htmlGeneratorFactory.Object,
                rdfResourceMapper, htmlResourceMapper);

        }

        public void Dispose()
        {
            _testDir.Delete(true);
        }

    }
}
