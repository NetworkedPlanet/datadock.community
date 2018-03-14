using System;
using System.IO;
using Datadock.Common.Models;
using FluentAssertions;
using NetworkedPlanet.Quince;
using VDS.RDF;
using Xunit;

namespace DataDock.Worker.Tests
{
    public class CsvConverionSpec
    {
        private readonly ContactInfo _publisher = new ContactInfo
        {
            Type = "http://xmlns.com/foaf/0.1/Organization",
            Email = "contact@networkedplanet.com",
            Label = "NetworkedPlanet",
            Website = "http://networkedplanet.com/"
        };

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

            var conversionProcessor =
                new ConversionJobProcessor(
                    new ConversionJobProcessorConfiguration {GitPath = "git", RepoBaseDir = "tmp//generate"}, null, null,
                    null, null, null, null, null, null, new MockProgressLog());
            conversionProcessor.GenerateRdf(repo, tmpDir, new Uri("http://example.org/"),
                new Uri[] {new Uri("http://example.org/g/g1")});

            var expectFile = new FileInfo(Path.Combine(tmpDir, "data","resource", "s", "s0.nq"));
            expectFile.Exists.Should().BeTrue();
            var unexpectFile = new FileInfo(Path.Combine(tmpDir, "data","resource", "s", "s1.rdf"));
            unexpectFile.Exists.Should().BeFalse();

#if GENERATE_RDFXML
            expectFile = new FileInfo(Path.Combine(tmpDir, "data", "s", "s0.rdf"));
            expectFile.Exists.Should().BeTrue();
            unexpectFile = new FileInfo(Path.Combine(tmpDir, "data", "s", "s1.rdf"));
            unexpectFile.Exists.Should().BeFalse();
#endif
        }

    }
}
