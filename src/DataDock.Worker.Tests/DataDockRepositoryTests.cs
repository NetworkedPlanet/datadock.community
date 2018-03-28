using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Datadock.Common.Models;
using FluentAssertions;
using Moq;
using NetworkedPlanet.Quince;
using VDS.RDF;
using Xunit;

namespace DataDock.Worker.Tests
{
    public class BasicDataDockRepositoryTests
    {
        private string _repoPath;

        private DataDockRepository _repo;
        private IGraph _insertGraph;
        private IGraph _metadataGraph;
        private IGraph _definitionsGraph;
        private Uri _datasetGraphIri;
        private Uri _metadataGraphIri;
        private Uri _definitionsGraphIri;
        private Uri _publisherIri;
        private ContactInfo _publisherInfo;
        private string _repositoryTitle;
        private string _repositoryDescription;
        private Uri _rootMetadataGraphIri;
        private MockQuinceStore _quinceStore;
        private Uri _baseUri;

        public BasicDataDockRepositoryTests()
        {
            var runId = DateTime.UtcNow.Ticks.ToString();
            var repoDir = Directory.CreateDirectory(runId);
            _repoPath = repoDir.FullName;

            _quinceStore = new MockQuinceStore();
            var quinceStoreFactory = new Mock<IQuinceStoreFactory>();
            quinceStoreFactory.Setup(x => x.MakeQuinceStore(_repoPath)).Returns(_quinceStore);
            var htmlGeneratorFactory = new Mock<IHtmlGeneratorFactory>();
            _baseUri = new Uri("http://datadock.io/test/repo");
            _repo = new DataDockRepository(_repoPath, _baseUri, new MockProgressLog(),
                quinceStoreFactory.Object, htmlGeneratorFactory.Object);
            _insertGraph = new Graph();
            _insertGraph.Assert(_insertGraph.CreateUriNode(new Uri("http://example.org/s")),
                _insertGraph.CreateUriNode(new Uri("http://example.org/p")),
                _insertGraph.CreateUriNode(new Uri("http://example.org/o")));
            _datasetGraphIri = new Uri("http://datadock.io/test/repo/example");
            _metadataGraph = new Graph();
            _metadataGraph.Assert(
                _metadataGraph.CreateUriNode(_datasetGraphIri),
                _metadataGraph.CreateUriNode(new Uri("http://example.org/properties/foo")),
                _metadataGraph.CreateLiteralNode("foo"));
            _metadataGraphIri= new Uri("http://datadock.io/test/repo/example/metadata");
            _definitionsGraph = new Graph();
            _definitionsGraph.Assert(
                _definitionsGraph.CreateUriNode(_datasetGraphIri),
                _definitionsGraph.CreateUriNode(new Uri("http://example.org/properties/bar")),
                _definitionsGraph.CreateLiteralNode("bar"));
            _definitionsGraphIri = new Uri("http://datadock.io/test/repo/example/definitions");
            _publisherIri = new Uri("http://datadock.io/test/publisher");
            _publisherInfo= new ContactInfo { Label = "Test Publisher" };
            _repositoryTitle = "Test Repository";
            _repositoryDescription = "Test Repository Description";
            _rootMetadataGraphIri = new Uri("http://datadock.io/test/repo/metadata");
        }

        [Fact]
        public void UpdateAssertsDataTriples()
        {
            _repo.UpdateDataset(_insertGraph, _datasetGraphIri, true,
                _metadataGraph, _metadataGraphIri,
                _definitionsGraph, _definitionsGraphIri,
                _publisherIri, _publisherInfo,
                _repositoryTitle, _repositoryDescription,
                _rootMetadataGraphIri);
            _quinceStore.AssertTriplesInserted(_insertGraph.Triples, _datasetGraphIri);
        }

        [Fact]
        public void UpdateCanDropDatasetGraph()
        {
            _repo.UpdateDataset(_insertGraph, _datasetGraphIri, true,
                _metadataGraph, _metadataGraphIri,
                _definitionsGraph, _definitionsGraphIri,
                _publisherIri, _publisherInfo,
                _repositoryTitle, _repositoryDescription,
                _rootMetadataGraphIri);
            _quinceStore.AssertTriplesInserted(_insertGraph.Triples, _datasetGraphIri);
            _quinceStore.DroppedGraphs.Should().Contain(_datasetGraphIri);
        }

        [Fact]
        public void UpdateCanAppendToExistingDatasetGraph()
        {
            _repo.UpdateDataset(_insertGraph, _datasetGraphIri, false,
                _metadataGraph, _metadataGraphIri,
                _definitionsGraph, _definitionsGraphIri,
                _publisherIri, _publisherInfo,
                _repositoryTitle, _repositoryDescription,
                _rootMetadataGraphIri);
            _quinceStore.AssertTriplesInserted(_insertGraph.Triples, _datasetGraphIri);
            _quinceStore.DroppedGraphs.Should().NotContain(_datasetGraphIri);
        }

        [Fact]
        public void UpdateDropsMetadataGraph()
        {
            _repo.UpdateDataset(_insertGraph, _datasetGraphIri, false,
                _metadataGraph, _metadataGraphIri,
                _definitionsGraph, _definitionsGraphIri,
                _publisherIri, _publisherInfo,
                _repositoryTitle, _repositoryDescription,
                _rootMetadataGraphIri);
            _quinceStore.AssertTriplesInserted(_insertGraph.Triples, _datasetGraphIri);
            _quinceStore.DroppedGraphs.Should().Contain(_metadataGraphIri);
        }

        [Fact]
        public void UpdateAssertsMetadataGraphTriples()
        {
            _repo.UpdateDataset(_insertGraph, _datasetGraphIri, true,
                _metadataGraph, _metadataGraphIri,
                _definitionsGraph, _definitionsGraphIri,
                _publisherIri, _publisherInfo,
                _repositoryTitle, _repositoryDescription,
                _rootMetadataGraphIri);
            _quinceStore.AssertTriplesInserted(_metadataGraph.Triples, _metadataGraphIri);
        }

        [Fact]
        public void UpdateAssertsDefinitionsGraphTriples()
        {
            _repo.UpdateDataset(_insertGraph, _datasetGraphIri, true,
                _metadataGraph, _metadataGraphIri,
                _definitionsGraph, _definitionsGraphIri,
                _publisherIri, _publisherInfo,
                _repositoryTitle, _repositoryDescription,
                _rootMetadataGraphIri);
            _quinceStore.AssertTriplesInserted(_metadataGraph.Triples, _metadataGraphIri);
        }

        [Fact]
        public void UpdateAddsMetadataToRootMetadataGraph()
        {
            _repo.UpdateDataset(_insertGraph, _datasetGraphIri, true,
                _metadataGraph, _metadataGraphIri,
                _definitionsGraph, _definitionsGraphIri,
                _publisherIri, _publisherInfo,
                _repositoryTitle, _repositoryDescription,
                _rootMetadataGraphIri);
            var expectedRootMetadata = new Graph();
            // Expectations
            var repoNode = expectedRootMetadata.CreateUriNode(_baseUri);
            // (repo, rdf:type, void:Dataset)
            expectedRootMetadata.Assert(
                repoNode,
                expectedRootMetadata.CreateUriNode(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type")),
                expectedRootMetadata.CreateUriNode(new Uri("http://rdfs.org/ns/void#Dataset")));
            // (repo, void:subset, dataset)
            expectedRootMetadata.Assert(
                repoNode,
                expectedRootMetadata.CreateUriNode(new Uri("http://rdfs.org/ns/void#subset")),
                expectedRootMetadata.CreateUriNode(_datasetGraphIri));
            // (repo, dcterms:publisher, publisher)
            expectedRootMetadata.Assert(
                repoNode,
                expectedRootMetadata.CreateUriNode(new Uri("http://purl.org/dc/terms/publisher")),
                expectedRootMetadata.CreateUriNode(_publisherIri));

            _quinceStore.AssertTriplesInserted(expectedRootMetadata.Triples, _rootMetadataGraphIri);
        }

        [Fact]
        public void UpdateAssertsTitleAndDescription()
        {
            _repo.UpdateDataset(_insertGraph, _datasetGraphIri, true,
                _metadataGraph, _metadataGraphIri,
                _definitionsGraph, _definitionsGraphIri,
                _publisherIri, _publisherInfo,
                _repositoryTitle, _repositoryDescription,
                _rootMetadataGraphIri);
            var expect = new Graph();
            expect.NamespaceMap.AddNamespace("dcterms", new Uri("http://purl.org/dc/terms/"));
            var repoNode = expect.CreateUriNode(_baseUri);
            expect.Assert(repoNode, expect.CreateUriNode("dcterms:title"), expect.CreateLiteralNode(_repositoryTitle));
            expect.Assert(repoNode, expect.CreateUriNode("dcterms:description"), expect.CreateLiteralNode(_repositoryDescription));
            _quinceStore.AssertTriplesInserted(expect.Triples, _rootMetadataGraphIri);
        }
    }

    public class MockQuinceStore: IQuinceStore {

        public List<Uri> DroppedGraphs { get; }
        public List<Tuple<INode, INode, INode, Uri>> Asserted { get; }
        public bool Flushed { get; private set; }

        public MockQuinceStore()
        {
            Asserted = new List<Tuple<INode, INode, INode, Uri>>();
            DroppedGraphs = new List<Uri>();
            Flushed = false;
        }

        public void Assert(INode subject, INode predicate, INode obj, Uri graph)
        {
            Asserted.Add(new Tuple<INode, INode, INode, Uri>(subject, predicate, obj, graph));
        }

        public void Retract(INode subject, INode predicate, INode obj, Uri graph)
        {
            throw new NotImplementedException();
        }

        public void DropGraph(Uri graph)
        {
            DroppedGraphs.Add(graph);
        }

        public void Flush()
        {
            Flushed = true;
        }

        public IEnumerable<Triple> GetTriplesForSubject(INode subjectNode)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Triple> GetTriplesForSubject(Uri subjectUri)
        {
            return Asserted.Where(x => x.Item1 is IUriNode && ((IUriNode)x.Item1).Uri.Equals(subjectUri))
                .Select(x => new Triple(x.Item1, x.Item2, x.Item3, x.Item4));
        }

        public IEnumerable<Triple> GetTriplesForObject(INode objectNode)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Triple> GetTriplesForObject(Uri objectUri)
        {
            throw new NotImplementedException();
        }

        public void EnumerateSubjects(ITripleCollectionHandler handler)
        {
            throw new NotImplementedException();
        }

        public void EnumerateSubjects(IResourceStatementHandler handler)
        {
            throw new NotImplementedException();
        }

        public void AssertTriplesInserted(BaseTripleCollection tripleCollection, Uri graphIri)
        {
            foreach (var t in tripleCollection)
            {
                Asserted.Should().Contain(x =>
                    x.Item1.Equals(t.Subject) && x.Item2.Equals(t.Predicate) && x.Item3.Equals(t.Object) &&
                    x.Item4.Equals(graphIri),
                    "Expected a quad ({0}, {1}, {2}, {3}) to have been asserted but no matching quad was found.",
                    t.Subject, t.Predicate, t.Object, graphIri);
            }
        }
    }
}
