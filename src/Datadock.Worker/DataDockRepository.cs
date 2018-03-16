using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DataDock.Worker.Templating;
using NetworkedPlanet.Quince;
using Serilog;
using VDS.RDF;

namespace DataDock.Worker
{
    public class DataDockRepository
    {
        private readonly string _targetDirectory;
        private readonly Uri _repositoryUri;
        private readonly IQuinceStore _quinceStore;
        private readonly IProgressLog _progressLog;
        private readonly IHtmlGeneratorFactory _htmlGeneratorFactory;

        /// <summary>
        /// How many files to generate between progress reports
        /// </summary>
        private const int RdfFileGenerationReportInterval = 500;

        /// <summary>
        /// How many files to generate between progress reports
        /// </summary>
        private const int HtmlFileGenerationReportInterval = 250;

        /// <summary>
        /// Create a new repository that updates the local clone of a DataDock GitHub repository
        /// </summary>
        /// <param name="targetDirectory">The path to the directory containing the local clone</param>
        /// <param name="repositoryUri">The base IRI for DataDock graphs in this repository</param>
        /// <param name="progressLog">The progress logger to report to</param>
        /// <param name="quinceStoreFactory">a factory for creating an IQuinceStore instance to access the Quince store of the GitHub repository</param>
        /// <param name="htmlFileGeneratorFactory">a factory for creating an <see cref="IHtmlGeneratorFactory"/> instance to generate the statically published HTML files for the GitHub repository</param>
        public DataDockRepository(string targetDirectory, Uri repositoryUri, IProgressLog progressLog,
            IQuinceStoreFactory quinceStoreFactory,
            IHtmlGeneratorFactory htmlFileGeneratorFactory)
        {
            _targetDirectory = targetDirectory;
            _repositoryUri = repositoryUri;
            _progressLog = progressLog;
            _quinceStore = quinceStoreFactory.MakeQuinceStore(targetDirectory);
            _htmlGeneratorFactory = htmlFileGeneratorFactory;
        }

        /// <summary>
        /// Remove a dataset contents and its metadata records from the repository
        /// </summary>
        /// <param name="datasetIri">The IRI of the dataset to be removed</param>
        public void DeleteDataset( Uri datasetIri)
        {
            Log.Information("DeleteDataset: {datasetIri}", datasetIri);
            var datasetMetadataGraphIri = new Uri(datasetIri + "/metadata");
            var rootMetadataGraphIri = new Uri(_repositoryUri, "metadata");

            _progressLog.Info("Dropping dataset graph {0}", datasetIri);
            _quinceStore.DropGraph(datasetIri);
            _progressLog.Info("Dropping dataset metadata graph {0}", datasetMetadataGraphIri);
            _quinceStore.DropGraph(datasetMetadataGraphIri);
            _progressLog.Info("Updating root metadata graph");
            var g = new Graph();
            var subset = g.CreateUriNode(new Uri("http://rdfs.org/ns/void#subset"));
            _quinceStore.Retract(g.CreateUriNode(_repositoryUri), subset, g.CreateUriNode(datasetIri), rootMetadataGraphIri);
            _progressLog.Info("Saving repository changes");
            _quinceStore.Flush();
        }

        /// <summary>
        /// Update or create the statically generated HTML and RDF for the DataDock repository
        /// </summary>
        public void Publish()
        {
            GenerateRdf(null);
            GenerateHtml();
            GenerateVoidMetadata();
        }

        public void GenerateRdf(IEnumerable<Uri> graphFilter)
        {
            try
            {
                var identifierUri = new Uri(_repositoryUri, "id/");
                var resourcePath = Path.Combine(_targetDirectory, "data");
                var rdfMapper = new ResourceFileMapper(new List<ResourceMapEntry>
                {
                    new ResourceMapEntry(identifierUri, resourcePath)
                });
                if (graphFilter == null)
                {
                    // Performing a complete reset of RDF data
                    _progressLog.Info("Performing a clean rebuild of data directory");
                    RemoveDirectory(resourcePath);
                }
                var rdfGenerator = new RdfFileGenerator(rdfMapper, graphFilter, _progressLog, RdfFileGenerationReportInterval);
                _quinceStore.EnumerateSubjects(rdfGenerator);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error running RDF file generation");
                throw new ConversionJobProcessorException(ex, "Error running RDF file generation. File generation may be incomplete.");
            }
        }


        private void GenerateHtml()
        {
            try
            {
                var templateEngine = new Liquid.LiquidViewEngine();
                var assemblyPath = Assembly.GetExecutingAssembly().Location;
                var templatePath = Path.Combine(Path.GetDirectoryName(assemblyPath), "templates");
                templateEngine.Initialize(templatePath, _quinceStore,
                    selectors: new List<ITemplateSelector>
                    {
                        new RdfTypeTemplateSelector(new Uri("http://rdfs.org/ns/void#Dataset"), "dataset.liquid")
                    });

                var identifierUri = new Uri(_repositoryUri, "id/");
                var resourcePath = Path.Combine(_targetDirectory, "page");
                var htmlMapper =
                    new ResourceFileMapper(new List<ResourceMapEntry>
                    {
                        new ResourceMapEntry(identifierUri, resourcePath)
                    });

                RemoveDirectory(resourcePath);
                var generator = _htmlGeneratorFactory.MakeHtmlFileGenerator(htmlMapper, templateEngine, _progressLog, HtmlFileGenerationReportInterval);

                _quinceStore.EnumerateSubjects(generator);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error running HTML file generation");
                throw new ConversionJobProcessorException(ex, "Error running HTML file generation. File generation may be incomplete.");
            }
        }

        private void GenerateVoidMetadata()
        {
            try
            {
                var templateEngine = new Liquid.LiquidViewEngine();
                var assemblyPath = Assembly.GetExecutingAssembly().Location;
                var templatePath = Path.Combine(Path.GetDirectoryName(assemblyPath), "templates");
                templateEngine.Initialize(templatePath, _quinceStore, "void.liquid");
                var voidGenerator = new VoidFileGenerator(templateEngine, _quinceStore, _repositoryUri, _progressLog);
                var htmlPath = Path.Combine(_targetDirectory, "page", "index.html");
                var nquadsPath = Path.Combine(_targetDirectory, "data", "void.nq");
                voidGenerator.GenerateVoidHtml(htmlPath);
                voidGenerator.GenerateVoidNQuads(nquadsPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error running metadata file generation");
                throw new ConversionJobProcessorException(ex, "Error running metadata file generation. File generation may be incomplete.");
            }
        }

        private static void RemoveDirectory(string directoryPath)
        {
            try
            {
                FileSystemHelper.DeleteDirectory(directoryPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to remove temporary directory {dirPath}", directoryPath);
            }
        }

    }
}
