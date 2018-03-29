using System;
using System.IO;
using FluentAssertions;
using Moq;
using NetworkedPlanet.Quince;
using VDS.RDF;
using Xunit;

namespace DataDock.Worker.Tests
{
    public class CsvConverionSpec
    {
        [Fact]
        public void CanGenerateRdfFromRepository()
        {
            var tmpDir = Path.GetFullPath("tmp//generate");
            if (Directory.Exists(tmpDir)) Directory.Delete(tmpDir, true);
            var repoDir = Path.Combine(tmpDir, "quince");
            Directory.CreateDirectory(repoDir);
            var repo = new DynamicFileStore(repoDir, 100);
            var defaultGraph = new Graph();
            var parser = new NQuadsParser();
            using (var reader = File.OpenText("data\\test1.nq"))
            {
                parser.Parse(reader, t => repo.Assert(t.Subject, t.Predicate, t.Object, t.GraphUri), defaultGraph);
            }

            repo.Flush();
            var mockQuinceFactory = new Mock<IQuinceStoreFactory>();
            mockQuinceFactory.Setup(x => x.MakeQuinceStore(It.IsAny<string>())).Returns(repo);
            var mockHtmlGeneratorFactory = new Mock<IHtmlGeneratorFactory>();

            var rdfResourceFileMapper = new ResourceFileMapper(
                new ResourceMapEntry(new Uri("http://example.org/id/"),  Path.Combine(tmpDir, "data")));
            var htmlResourceFileMapper = new ResourceFileMapper(
                new ResourceMapEntry(new Uri("http://example.org/id/"), Path.Combine(tmpDir, "doc")));
            var ddRepository = new DataDockRepository(
                tmpDir, new Uri("http://example.org/"), new MockProgressLog(),
                mockQuinceFactory.Object, mockHtmlGeneratorFactory.Object, 
                rdfResourceFileMapper, htmlResourceFileMapper);
            ddRepository.GenerateRdf(new[] {new Uri("http://example.org/g/g1")});

            var expectFile = new FileInfo(Path.Combine(tmpDir, "data", "resource", "s", "s0.nq"));
            expectFile.Exists.Should().BeTrue();
            var unexpectFile = new FileInfo(Path.Combine(tmpDir, "data", "resource", "s", "s1.rdf"));
            unexpectFile.Exists.Should().BeFalse();
        }
    }
}
