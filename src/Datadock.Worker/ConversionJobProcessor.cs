using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Datadock.Common;
using Datadock.Common.Models;
using Datadock.Common.Repositories;
using Datadock.Worker.Templating;
using DataDock.Common;
using DataDock.CsvWeb.Metadata;
using DataDock.CsvWeb.Parsing;
using DataDock.CsvWeb.Rdf;
using Medallion.Shell;
using NetworkedPlanet.Quince;
using NetworkedPlanet.Quince.Git;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Octokit;
using Serilog;
using VDS.RDF;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Writing;
using FileMode = System.IO.FileMode;
using Graph = VDS.RDF.Graph;

namespace Datadock.Worker
{
    public class ConversionJobProcessor : IProgress<int>
    {
        private readonly ConversionJobProcessorConfiguration _configuration;
        private readonly IFileStore _jobFileStore;
        private readonly IUserRepository _userRepository;
        private readonly IHtmlGeneratorFactory _htmlGeneratorFactory;
        private readonly IDatasetRepository _datasetRepository;
        private readonly ISchemaRepository _schemaRepository;
        private readonly IRepoSettingsRepository _repoSettingsRepository;
        private readonly IOwnerSettingsRepository _ownerSettingsRepository;
        private readonly IGitHubClientFactory _gitHubClientFactory;
        private readonly IProgressLog _progressLog;

        /// <summary>
        /// Used to generate a progress message during CSV->RDF conversion once for every X rows of CSV processed
        /// </summary>
        private const int CsvConversionReportInterval = 250;

        /// <summary>
        /// How many files to generate between progress reports
        /// </summary>
        private const int RdfFileGenerationReportInterval = 500;

        /// <summary>
        /// How many HTML files to generate between progress reports
        /// </summary>
        private const int HtmlFileGenerationReportInterval = 250;

        private const int QuinceCacheThreshold = 100;

        public ConversionJobProcessor(ConversionJobProcessorConfiguration configuration,
            IFileStore jobFileStore,
            IUserRepository userRepository,
            IOwnerSettingsRepository ownerSettingsRepository,
            IRepoSettingsRepository repoSettingsRepository,
            IHtmlGeneratorFactory htmlGeneratorFactory,
            IDatasetRepository datasetRepository,
            ISchemaRepository schemaRepository,
            IGitHubClientFactory gitHubClientFactory,
            IProgressLog progressLog)
        {
            _configuration = configuration;
            _jobFileStore = jobFileStore;
            _userRepository = userRepository;
            _ownerSettingsRepository = ownerSettingsRepository;
            _repoSettingsRepository = repoSettingsRepository;
            _htmlGeneratorFactory = htmlGeneratorFactory;
            _datasetRepository = datasetRepository;
            _schemaRepository = schemaRepository;
            _gitHubClientFactory = gitHubClientFactory;
            _progressLog = progressLog;
        }

        public async Task<bool> ProcessAsync(JobInfo job)
        {
            _progressLog.UpdateStatus(JobStatus.Running, "Started job processing");
            var sw = new Stopwatch();
            sw.Start();
            var targetDirectory = Path.Combine(_configuration.RepoBaseDir, job.JobId);

            // Get the user's name and email information
            var userAccount = await _userRepository.GetUserAccountAsync(job.UserId);
            if (userAccount == null)
            {
                _progressLog.Error("Cannot retrieve user information. Please check your DataDock account settings");
                return false;
            }

            // Get the user's github authentication token
            var authenticationToken =
                userAccount.Claims.FirstOrDefault(c => c.Type.Equals(DataDockClaimTypes.GitHubAccessToken))?.Value;
            if (string.IsNullOrEmpty(authenticationToken))
            {
                _progressLog.Error(
                    "Cannot retrieve user authentication token. Please check that your DataDock account is still connected to your GitHub account.");
                return false;
            }

            try
            {
                switch (job.JobType)
                {
                    case JobType.Import:
                        await ProcessImportJob(job, targetDirectory, userAccount, authenticationToken);
                        break;
                    case JobType.Delete:
                        await ProcessDeleteJob(job, targetDirectory, userAccount, authenticationToken);
                        break;
                    case JobType.SchemaCreate:
                        _progressLog.Info("Creating new entry in Schema Library");

                        await ProcessSchemaCreateJob(job);
                        break;
                    case JobType.SchemaDelete:
                        _progressLog.Info("Removing entry from Schema Library");
                        await ProcessSchemaDeleteJob(job);
                        break;
                }
                sw.Stop();
                _progressLog.UpdateStatus(JobStatus.Completed,
                    $"Processing of job completed in {sw.Elapsed.TotalSeconds:F2} seconds");
                return true;
            }
            catch (ConversionJobProcessorException ex)
            {
                _progressLog.UpdateStatus(JobStatus.Failed, ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unhandled exception in converison processing");
                _progressLog.UpdateStatus(JobStatus.Failed,
                    "Conversion processing failed with an unexpected error. Cause: {0}", ex.Message);
                return false;
            }
            finally
            {
                // Clean-up (Note that failure here is not propagated as we don't want to fail the job)
                await Task.Run(() => { RemoveDirectory(targetDirectory); });
            }
        }

        private async Task<ContactInfo> GetPublisherContactInfo(string ownerId, string repoId)
        {
            try
            {
                _progressLog.Info("Attempting to retrieve publisher contact information from repository settings");
                // get repoSettings
                var repoSettings = await _repoSettingsRepository.GetRepoSettingsAsync(ownerId, repoId);
                if (repoSettings?.DefaultPublisher != null)
                {
                    _progressLog.Info("Returning publisher from repository settings");
                    return repoSettings.DefaultPublisher;
                }
                // no repo settings publisher, try at owner level
                _progressLog.Info("No publisher info found in repository settings");
                if (ownerId != null)
                {
                    _progressLog.Info("Attempting to retrieve publisher contact information from repository owner's settings");
                    var ownerSettings = await _ownerSettingsRepository.GetOwnerSettingsAsync(ownerId);
                    if (ownerSettings?.DefaultPublisher != null)
                    {
                        _progressLog.Info("Returning publisher from repository owner's settings");
                        return ownerSettings.DefaultPublisher;
                    }
                }
                // no settings / publisher found for that repo
                _progressLog.Info("No publisher info found in repository owner's settings");
                return null;
            }
            catch (Exception)
            {
                _progressLog.Error("Error when attempting to retrieve publisher contact information from repository/owner settings");
                return null;
            }

        }

        private async Task ProcessImportJob(JobInfo job, string targetDirectory, UserAccount userAccount, string authenticationToken)
        {
            // Clone the repository
            await CloneRepository(job.GitRepositoryUrl, targetDirectory, authenticationToken, userAccount);

            // Retrieve CSV and CSVM files to src directory in the repository
            await AddCsvFilesToRepository(targetDirectory, 
                job.DatasetId,
                job.CsvFileName, 
                job.CsvFileId, 
                job.CsvmFileId);

            var csvPath = Path.Combine(targetDirectory, "csv", job.DatasetId, job.CsvFileName);
            var metaPath = Path.Combine(targetDirectory, "csv", job.DatasetId, job.CsvFileName+ "-metadata.json");

            // Parse the JSON metadata
            JObject metadataJson;
            using (var metadataReader = File.OpenText(metaPath))
            {
                var metadataString = metadataReader.ReadToEnd();
                metadataJson = JObject.Parse(metadataString);
            }

            // Run the CSV to RDF conversion
            var repositoryUri = new Uri(DataDockUrlHelper.GetRepositoryUri(job.GitRepositoryFullName));
            var publisherIri = new Uri(repositoryUri, "id/dataset/publisher");
            var datasetUri = new Uri(job.DatasetIri);
            var datasetMetadataGraphIri = new Uri(datasetUri + "/metadata");
            var rootMetadataGraphIri = new Uri(repositoryUri, "metadata");
            var definitionsGraphIri = new Uri(repositoryUri, "definitions");
            var dateTag = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var releaseTag = MakeSafeTag(job.DatasetId + "_" + dateTag);
            var publisher = await GetPublisherContactInfo(job.OwnerId, job.RepositoryId);
            var ntriplesDownloadLink =
                new Uri($"https://github.com/{job.GitRepositoryFullName}/releases/download/{releaseTag}/{releaseTag}.nt.gz");
            var csvDownloadLink =
                new Uri(repositoryUri + $"csv/{job.DatasetId}/{job.CsvFileName}");

            IGraph datasetGraph;
            using (var csvReader = File.OpenText(csvPath))
            {
                datasetGraph = GenerateDatasetGraph(csvReader, metadataJson);
            }
            IGraph metadataGraph = GenerateMetadataGraph(datasetUri, publisherIri, metadataJson,
                new [] { ntriplesDownloadLink, csvDownloadLink }, datasetGraph);

            IGraph definitionsGraph = GenerateDefinitionsGraph(metadataJson);

            UpdateRepository(targetDirectory, datasetGraph, metadataGraph, definitionsGraph, datasetUri, repositoryUri, publisherIri,
                "", "",
                rootMetadataGraphIri, datasetMetadataGraphIri, definitionsGraphIri, publisher, job.OverwriteExistingData);

            var quincePath = Path.Combine(targetDirectory, "quince");
            var quinceStore = new DynamicFileStore(quincePath, QuinceCacheThreshold);

            // Run the RDF aggregation to generate .rdf/.ttl files for individual subjects
            GenerateRdf(quinceStore, targetDirectory, repositoryUri,
                new[] { datasetUri, datasetMetadataGraphIri, rootMetadataGraphIri });

            // Run the RDF to HTML conversion to generate .html files for individual subjects
            GenerateHtml(quinceStore, targetDirectory, repositoryUri);

            GenerateVoidMetadata(quinceStore, targetDirectory, repositoryUri);

            // Add and Commit all changes
            if (await CommitChanges(targetDirectory,
                $"Added {job.CsvFileName} to dataset {job.DatasetIri}", userAccount))
            {
                await PushChanges(job.GitRepositoryUrl, targetDirectory, authenticationToken);
                await MakeRelease(datasetGraph, releaseTag, job.OwnerId, job.RepositoryId,
                    job.DatasetId, targetDirectory, authenticationToken);
            }

            // Update the dataset repository
            try
            {
                await _datasetRepository.CreateOrUpdateDatasetRecordAsync(new DatasetInfo
                {
                    OwnerId = job.OwnerId,
                    RepositoryId = job.GitRepositoryFullName,
                    DatasetId = job.DatasetId,
                    LastModified = DateTime.UtcNow,
                    Metadata = metadataJson,
                    ShowOnHomePage = job.IsPublic
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to update dataset record");
                throw new ConversionJobProcessorException(ex,
                    "Failed to update dataset record. Your repository is updated, but may not show in the main lodlab portal.");
            }
        }

        private string MakeSafeTag(string tag)
        {
            return Regex.Replace(tag, @"[^a-zA-Z0-9]", "_", RegexOptions.None);
        }

        private async Task ProcessDeleteJob(JobInfo job, string targetDirectory, UserAccount userAccount, string authenticationToken)
        {
            await CloneRepository(job.GitRepositoryUrl, targetDirectory, authenticationToken, userAccount);
            var repositoryUri = new Uri(DataDockUrlHelper.GetRepositoryUri(job.GitRepositoryFullName));
            var datasetUri = new Uri(job.DatasetIri);
            var quincePath = Path.Combine(targetDirectory, "quince");
            var quinceStore = new DynamicFileStore(quincePath, 10);

            DeleteDataset(quinceStore, datasetUri, repositoryUri);

            DeleteCsvAndMetadata(targetDirectory, job.DatasetId);
            GenerateRdf(quinceStore, targetDirectory, repositoryUri, null);
            GenerateHtml(quinceStore, targetDirectory, repositoryUri);
            GenerateVoidMetadata(quinceStore, targetDirectory, repositoryUri);
            if (await CommitChanges(targetDirectory,
                $"Deleted dataset {datasetUri}", userAccount))
            {
                await PushChanges(job.GitRepositoryUrl, targetDirectory, authenticationToken);
            }
            try
            {
                await _datasetRepository.DeleteDatasetAsync(job.GitRepositoryFullName, job.DatasetId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to remove dataset record.");
                throw new ConversionJobProcessorException(ex, "Failed to remove dataset record. Your repository is updated but the dataset may still show in the main lodlab portal");
            }
        }

        private static string PrintObjectProperties(object obj)
        {
            var sb = new StringBuilder();
            try
            {
                if (obj == null) return "null";

                foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(obj))
                {
                    string name = descriptor.Name;
                    object value = descriptor.GetValue(obj);
                    sb.Append(string.Format("{0}={1}", name, value));
                }

            }
            catch (Exception e)
            {
                sb.Append(string.Format("Error: {0}", e.Message));
            }
            return sb.ToString();
        }

        private async Task ProcessSchemaCreateJob(JobInfo job)
        {
            // Save the schema to documentDB
            try
            {
                Log.Debug("Create schema. Schema file Id: {schemaFileId}", job.SchemaFileId);
                _progressLog.UpdateStatus(JobStatus.Running, "Create schema. Job details: {0}", PrintObjectProperties(job));
                // get schema from file store
                if (!string.IsNullOrEmpty(job.SchemaFileId))
                {
                    // Parse the JSON metadata
                    JObject schemaJson;
                    var schemaFileStream = await _jobFileStore.GetFileAsync(job.SchemaFileId);
                    var serializer = new JsonSerializer();

                    using (var sr = new StreamReader(schemaFileStream))
                    using (var jsonTextReader = new JsonTextReader(sr))
                    {
                        schemaJson = serializer.Deserialize(jsonTextReader) as JObject;
                    }
                    if (schemaJson != null)
                    {
                        _progressLog.UpdateStatus(JobStatus.Running, "Schema JSON retrieved from file system: {0}",
                            schemaJson);

                        Log.Debug("Create schema: OwnerId: {ownerId} RepositoryId: {repoId} SchemaFileId: {schemaFileId}",
                            job.OwnerId, job.GitRepositoryFullName, job.SchemaFileId);
                        var schemaInfo = new SchemaInfo
                        {
                            OwnerId = job.OwnerId,
                            RepositoryId = job.GitRepositoryFullName,
                            LastModified = DateTime.UtcNow,
                            SchemaId = Guid.NewGuid().ToString(),
                            Schema = schemaJson,
                        };
                        _progressLog.UpdateStatus(JobStatus.Running,
                            "Creating schema record... Schema info details: {0}", PrintObjectProperties(schemaInfo));

                        await _schemaRepository.CreateOrUpdateSchemaRecordAsync(schemaInfo);
                        _progressLog.UpdateStatus(JobStatus.Running, "Schema record created successfully.");
                    }
                    else
                    {
                        _progressLog.UpdateStatus(JobStatus.Failed,
                            "Unable to create schema - unable to retrieve schema JSON from temporary file storage");
                        throw new ConversionJobProcessorException(
                            "Unable to create schema - unable to retrieve schema JSON from temporary file storage");
                    }
                }
                else
                {
                    _progressLog.UpdateStatus(JobStatus.Failed, "Unable to create schema - missing file Id");
                    throw new ConversionJobProcessorException("Unable to create schema - missing file Id");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to update schema record");
                _progressLog.UpdateStatus(JobStatus.Failed, "Failed to update schema record");

                throw new ConversionJobProcessorException(ex,
                    "Failed to update schema record.");
            }
        }


        private async Task ProcessSchemaDeleteJob(JobInfo job)
        {
            // Delete the schema from documentDB
            try
            {
                await _schemaRepository.DeleteSchemaAsync(null, job.SchemaId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to remove schema record");
                throw new ConversionJobProcessorException(ex,
                    "Failed to delete schema record.");
            }
        }

        private void DeleteCsvAndMetadata(string baseDirectory, string datasetId)
        {
            try
            {
                _progressLog.Info("Deleting source CSV and CSV metadata files");
                var csvPath = Path.Combine(baseDirectory, "csv", datasetId);
                Directory.Delete(csvPath, true);
            }
            catch (Exception ex)
            {
                _progressLog.Exception(ex, "Error deleting source CSV and CSV metadata files");
                throw;
            }
        }

        private void DeleteDataset(IQuinceStore quinceStore, Uri datasetUri, Uri repositoryUri)
        {
            var datasetMetadataGraphIri = new Uri(datasetUri + "/metadata");
            var rootMetadataGraphIri = new Uri(repositoryUri, "metadata");

            _progressLog.Info("Dropping dataset graph {0}", datasetUri);
            quinceStore.DropGraph(datasetUri);
            _progressLog.Info("Dropping dataset metadata graph {0}", datasetMetadataGraphIri);
            quinceStore.DropGraph(datasetMetadataGraphIri);
            _progressLog.Info("Updating root metadata graph");
            var g = new Graph();
            var subset = g.CreateUriNode(new Uri("http://rdfs.org/ns/void#subset"));
            quinceStore.Retract(g.CreateUriNode(repositoryUri), subset, g.CreateUriNode(datasetUri), rootMetadataGraphIri);
            _progressLog.Info("Saving repository changes");
            quinceStore.Flush();
        }

        private async Task AddCsvFilesToRepository(string repositoryDirectory, string datasetId, string csvFileName, string csvFileId, string csvmFileId)
        {
            try
            {
                _progressLog.Info("Copying source CSV and metadata files to repository directory csv/{0}", datasetId);
                var datasetCsvDirPath = Path.Combine(repositoryDirectory, "csv", datasetId);
                if (!Directory.Exists(datasetCsvDirPath)) Directory.CreateDirectory(datasetCsvDirPath);
                var csvFilePath = Path.Combine(datasetCsvDirPath, csvFileName);
                var csvFileStream = await _jobFileStore.GetFileAsync(csvFileId);
                using (var csvOutStream = File.Open(csvFilePath, FileMode.Create, FileAccess.Write))
                {
                    csvFileStream.CopyTo(csvOutStream);
                }
                if (csvmFileId != null)
                {
                    var csvmFilePath = csvFilePath + "-metadata.json";
                    var csvmFileStream = await _jobFileStore.GetFileAsync(csvmFileId);
                    using (var csvmOutStream = File.Open(csvmFilePath, FileMode.Create, FileAccess.Write))
                    {
                        csvmFileStream.CopyTo(csvmOutStream);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to copy CSV/CSVM files");
                throw new ConversionJobProcessorException(ex, "Failed to copy CSV/CSVM files from upload to Github repository.");
            }
        }

        private Graph GenerateDatasetGraph(TextReader csvReader, JObject metadataJson)
        {
            var parser = new JsonMetadataParser(null);
            Table tableMeta;
            try
            {
                tableMeta = parser.Parse(metadataJson) as Table;
                if (tableMeta == null)
                {
                    throw new ConversionJobProcessorException("CSV Conversion failed. Unable to read CSV table metadata.");
                }
            }
            catch (MetadataParseException ex)
            {
                Log.Error(ex, "Unable to parse CSV table metadata");
                throw new ConversionJobProcessorException(ex, "CSV conversion failed. Unable to parse CSV table metadata.");
            }

            var graph = new Graph();
            _progressLog.Info("Running CSV to RDF conversion");
            var graphHandler = new GraphHandler(graph);
            var converter = new Converter(tableMeta, graphHandler, (msg) => _progressLog.Error(msg), this, reportInterval: CsvConversionReportInterval);
            converter.Convert(csvReader);
            if (converter.Errors.Any())
            {
                throw new ConversionJobProcessorException("One or more errors where encountered during the CSV to RDF conversion.");
            }
            return graph;
        }

        private Graph GenerateMetadataGraph(Uri datasetUri, Uri publisherIri, JObject metadataJson, IEnumerable<Uri> downloadUris, IGraph dataGraph)
        {
            var metadataGraph = new Graph();
            var metadataExtractor = new MetdataExtractor();
            _progressLog.Info("Extracting dataset metadata");
            metadataExtractor.Run(metadataJson, metadataGraph, publisherIri, dataGraph.Triples.Count, DateTime.UtcNow);
            var dsNode = metadataGraph.CreateUriNode(datasetUri);
            var ddNode = metadataGraph.CreateUriNode(new Uri("http://rdfs.org/ns/void#dataDump"));
            var exampleResource = metadataGraph.CreateUriNode(new Uri("http://rdfs.org/ns/void#exampleResource"));
            foreach (var downloadUri in downloadUris)
            {
                metadataGraph.Assert(dsNode, ddNode, metadataGraph.CreateUriNode(downloadUri));
            }
            foreach (var distinctSubject in dataGraph.Triples.Select(t => t.Subject).OfType<IUriNode>().Distinct().Take(10))
            {
                metadataGraph.Assert(dsNode, exampleResource, distinctSubject);
            }
            return metadataGraph;
        }

        private Graph GenerateDefinitionsGraph(JObject metadataJson)
        {
            var definitionsGraph = new Graph();
            var metadataExtractor = new MetdataExtractor();
            _progressLog.Info("Extracting column property definitions");
            metadataExtractor.GenerateColumnDefinitions(metadataJson, definitionsGraph);
            return definitionsGraph;
        }

        private void UpdateRepository(string repositoryDirectory,
            IGraph graph, IGraph metadataGraph, IGraph definitionsGraph,
            Uri datasetIri, Uri repositoryIri, Uri publisherIri,
            string repositoryTitle, string repositoryDescription,
            Uri rootMetadataGraphIri, Uri metadataGraphIri, Uri definitionsGraphIri,
            ContactInfo publisherInfo, bool dropExistingGraph = true)
        {

            try
            {
                _progressLog.Info("Updating RDF repository");
                var quincePath = Path.Combine(repositoryDirectory, "quince");
                if (!Directory.Exists(quincePath)) Directory.CreateDirectory(quincePath);
                var quinceStore = new DynamicFileStore(quincePath, 100);

                if (dropExistingGraph)
                {
                    // Drop dataset data graph
                    _progressLog.Info("Dropping all existing RDF data in graph {0}", datasetIri);
                    quinceStore.DropGraph(datasetIri);
                }

                // Drop dataset metadata graph
                _progressLog.Info("Dropping all metadata in graph {0}", metadataGraphIri);
                quinceStore.DropGraph(metadataGraphIri);

                // Add triples to dataset data graph
                _progressLog.Info("Adding new RDF data to graph {0} ({1} triples)", datasetIri, graph.Triples.Count);
                var addCount = 0;
                foreach (var t in graph.Triples)
                {
                    quinceStore.Assert(t.Subject, t.Predicate, t.Object, datasetIri);
                    addCount++;
                    if (addCount % 1000 == 0)
                    {
                        _progressLog.Info("Added {0} / {1} triples ({2}%)", addCount, graph.Triples.Count,
                            addCount * 100 / graph.Triples.Count);
                    }
                }

                // Add triples to dataset metadata graph
                _progressLog.Info("Adding dataset metadata to graph {0} ({1} triples)", metadataGraphIri, metadataGraph.Triples.Count);
                foreach (var t in metadataGraph.Triples)
                {
                    quinceStore.Assert(t.Subject, t.Predicate, t.Object, metadataGraphIri);
                }

                // Add triples to the definitions graph
                _progressLog.Info("Adding column definition metadata to graph {0} ({1} triples)", definitionsGraphIri, definitionsGraph.Triples.Count);
                foreach (var t in definitionsGraph.Triples)
                {
                    quinceStore.Assert(t.Subject, t.Predicate, t.Object, definitionsGraphIri);
                }

                // Update the root metadata graph to ensure it includes this dataset as a subset
                _progressLog.Info("Updating root metadata");
                var repositoryNode = graph.CreateUriNode(repositoryIri);
                var subset = graph.CreateUriNode(new Uri("http://rdfs.org/ns/void#subset"));
                var rdfType = graph.CreateUriNode(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type"));
                var rdfsLabel = graph.CreateUriNode(new Uri("http://www.w3.org/2000/01/rdf-schema#label"));
                var foafName = graph.CreateUriNode(new Uri("http://xmlns.com/foaf/0.1/name"));
                var foafMbox = graph.CreateUriNode(new Uri("http://xmlns.com/foaf/0.1/mbox"));
                var foafHomepage = graph.CreateUriNode(new Uri("http://xmlns.com/foaf/0.1/homepage"));
                var dctermsPublisher = graph.CreateUriNode(new Uri("http://purl.org/dc/terms/publisher"));
                var dctermsTitle = graph.CreateUriNode(new Uri("http://purl.org/dc/terms/title"));
                var dctermsDescription = graph.CreateUriNode(new Uri("http://purl.org/dc/terms/description"));
                var publisherNode = graph.CreateUriNode(publisherIri);
                quinceStore.Assert(repositoryNode, rdfType, graph.CreateUriNode(new Uri("http://rdfs.org/ns/void#Dataset")), rootMetadataGraphIri);
                quinceStore.Assert(repositoryNode, subset, graph.CreateUriNode(datasetIri), rootMetadataGraphIri);
                quinceStore.Assert(repositoryNode, dctermsPublisher, publisherNode, rootMetadataGraphIri);


                // Update repository title and description
                foreach (var t in quinceStore.GetTriplesForSubject(repositoryIri))
                {
                    if (t.GraphUri.Equals(rootMetadataGraphIri))
                    {
                        if (t.Predicate.Equals(dctermsTitle) || t.Predicate.Equals(dctermsDescription))
                        {
                            quinceStore.Retract(t.Subject, t.Predicate, t.Object, rootMetadataGraphIri);
                        }
                    }
                }
                if (!string.IsNullOrEmpty(repositoryTitle))
                {
                    quinceStore.Assert(repositoryNode, dctermsTitle, graph.CreateLiteralNode(repositoryTitle), rootMetadataGraphIri);
                }
                if (!string.IsNullOrEmpty(repositoryDescription))
                {
                    quinceStore.Assert(repositoryNode, dctermsDescription, graph.CreateLiteralNode(repositoryDescription), rootMetadataGraphIri);
                }

                // Update publisher information
                foreach (var t in quinceStore.GetTriplesForSubject(publisherIri))
                {
                    if (t.GraphUri.Equals(rootMetadataGraphIri))
                    {
                        quinceStore.Retract(t.Subject, t.Predicate, t.Object, rootMetadataGraphIri);
                    }
                }
                if (!string.IsNullOrEmpty(publisherInfo?.Type))
                {
                    var publisherType = graph.CreateUriNode(new Uri(publisherInfo.Type));
                    quinceStore.Assert(publisherNode, rdfType, publisherType, rootMetadataGraphIri);
                }
                if (!string.IsNullOrEmpty(publisherInfo?.Label))
                {
                    var publisherLabel = graph.CreateLiteralNode(publisherInfo.Label);
                    quinceStore.Assert(publisherNode, rdfsLabel, publisherLabel, rootMetadataGraphIri);
                    quinceStore.Assert(publisherNode, foafName, publisherLabel, rootMetadataGraphIri);
                }
                if (!string.IsNullOrEmpty(publisherInfo?.Email))
                {
                    quinceStore.Assert(publisherNode, foafMbox, graph.CreateLiteralNode(publisherInfo.Email), rootMetadataGraphIri);
                }
                if (!string.IsNullOrEmpty(publisherInfo?.Website))
                {
                    quinceStore.Assert(publisherNode, foafHomepage, graph.CreateUriNode(new Uri(publisherInfo.Website)), rootMetadataGraphIri);
                }

                // Flush all data to disk
                quinceStore.Flush();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CSV conversion failed");
                throw new ConversionJobProcessorException(ex, "CSV conversion failed. Error updating RDF repository.");
            }
        }

        public void GenerateRdf(IQuinceStore quinceStore, string repositoryDirectory, Uri repositoryUri, IEnumerable<Uri> graphFilter)
        {
            try
            {
                var identifierUri = new Uri(repositoryUri, "id/");
                var resourcePath = Path.Combine(repositoryDirectory, "data");
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
                quinceStore.EnumerateSubjects(rdfGenerator);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error running RDF file generation");
                throw new ConversionJobProcessorException(ex, "Error running RDF file generation. File generation may be incomplete.");
            }
        }

        private void GenerateHtml(IQuinceStore quinceStore, string repositoryDirectory, Uri repositoryUri)
        {
            try
            {
                var templateEngine = new Liquid.LiquidViewEngine();
                var assemblyPath = Assembly.GetExecutingAssembly().Location;
                var templatePath = Path.Combine(Path.GetDirectoryName(assemblyPath), "templates");
                templateEngine.Initialize(templatePath, quinceStore,
                    selectors: new List<ITemplateSelector>
                    {
                        new RdfTypeTemplateSelector(new Uri("http://rdfs.org/ns/void#Dataset"), "dataset.liquid")
                    });

                var identifierUri = new Uri(repositoryUri, "id/");
                var resourcePath = Path.Combine(repositoryDirectory, "page");
                var htmlMapper =
                    new ResourceFileMapper(new List<ResourceMapEntry>
                    {
                        new ResourceMapEntry(identifierUri, resourcePath)
                    });

                RemoveDirectory(resourcePath);
                var generator = _htmlGeneratorFactory.MakeHtmlFileGenerator(htmlMapper, templateEngine, _progressLog, HtmlFileGenerationReportInterval);

                quinceStore.EnumerateSubjects(generator);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error running HTML file generation");
                throw new ConversionJobProcessorException(ex, "Error running HTML file generation. File generation may be incomplete.");
            }
        }

        private void GenerateVoidMetadata(IQuinceStore quinceStore, string repositoryDirectory, Uri repositoryUri)
        {
            try
            {
                var templateEngine = new Liquid.LiquidViewEngine();
                var assemblyPath = Assembly.GetExecutingAssembly().Location;
                var templatePath = Path.Combine(Path.GetDirectoryName(assemblyPath), "templates");
                templateEngine.Initialize(templatePath, quinceStore, "void.liquid");
                var voidGenerator = new VoidFileGenerator(templateEngine, quinceStore, repositoryUri, _progressLog);
                var htmlPath = Path.Combine(repositoryDirectory, "page", "index.html");
                var nquadsPath = Path.Combine(repositoryDirectory, "data", "void.nq");
                voidGenerator.GenerateVoidHtml(htmlPath);
                voidGenerator.GenerateVoidNQuads(nquadsPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error running metadata file generation");
                throw new ConversionJobProcessorException(ex, "Error running metadata file generation. File generation may be incomplete.");
            }
        }

        private async Task<bool> CommitChanges(string repositoryDirectory, string commitMessage, UserAccount userAccount)
        {
            var nameClaim = userAccount.Claims.FirstOrDefault(c=>c.Type.Equals(ClaimTypes.Name));
            var emailClaim = userAccount.Claims.FirstOrDefault(c=>c.Type.Equals(ClaimTypes.Email));
            // TODO: If nameClaim or emailClaim is null the user account is not properly configured
            var commitAuthor = nameClaim.Value + " <" + emailClaim.Value + ">";

            var git = new GitWrapper(repositoryDirectory, _configuration.GitPath);
            _progressLog.Info("Adding files to git repository.");
            var configResult = await git.SetUserName(nameClaim.Value);
            if (!configResult.Success)
            {
                throw new ConversionJobProcessorException("Commit failed: Could not configure git user name");
            }
            configResult = await git.SetUserEmail(emailClaim.Value);
            if (!configResult.Success)
            {
                throw new ConversionJobProcessorException("Commit failed: Could not configure git user email");
            }

            var addResult = await git.AddAll();
            if (!addResult.Success)
            {
                _progressLog.Error("git-add command failed with exit code {0}. Detail: {1}\n{2}", addResult.ExitCode, addResult.StandardOutput, addResult.StandardError);
                if (!string.IsNullOrEmpty(addResult.StandardOutput))
                {
                    _progressLog.Info("git-add command output: {0}", addResult.StandardOutput);
                }
                if (!string.IsNullOrEmpty(addResult.StandardError))
                {
                    _progressLog.Info("git-add command error output: {0}", addResult.StandardError);
                }
                throw new ConversionJobProcessorException("Commit failed: Failed to add modified files to local git working tree.");
            }

            var statusResult = await git.Status();
            if (!statusResult.Success)
            {
                _progressLog.Error("git-status command failed with exit code {0}", statusResult.CommandResult.ExitCode);
                if (!string.IsNullOrEmpty(statusResult.CommandResult.StandardOutput))
                {
                    _progressLog.Info("git-status command output: {0}", statusResult.CommandResult.StandardOutput);
                }
                if (!string.IsNullOrEmpty(statusResult.CommandResult.StandardError))
                {
                    _progressLog.Info("git-status command error output: {0}", statusResult.CommandResult.StandardError);
                }
                throw new ConversionJobProcessorException("Commit failed: Could not determine current state of local git working tree.");
            }

            // Commit only if there are some changes
            if (statusResult.DeletedFiles.Any() || statusResult.ModifiedFiles.Any() || statusResult.NewFiles.Any())
            {
                var commitResult = await git.Commit(subject: commitMessage, author: commitAuthor);
                if (!commitResult.Success)
                {
                    _progressLog.Error("git-commit command failed with exit code {0}.", commitResult.ExitCode,
                        commitResult.StandardOutput, commitResult.StandardError);
                    if (!string.IsNullOrEmpty(commitResult.StandardOutput))
                    {
                        _progressLog.Info("git-commit command output: {0}", commitResult.StandardOutput);
                    }
                    if (!string.IsNullOrEmpty(commitResult.StandardError))
                    {
                        _progressLog.Info("git-commit command error output: {0}", commitResult.StandardError);
                    }
                    throw new ConversionJobProcessorException("Commit failed: Git commit failed.");
                }
            }
            else
            {
                _progressLog.Info("No changes to commit");
                return false;
            }
            return true;
        }

        private async Task<bool> EnsureRepository(string repository, string targetDirectory, string authenticationToken, UserAccount userAccount)
        {
            var repoDir = Path.Combine(_configuration.RepoBaseDir, targetDirectory);
            Directory.CreateDirectory(repoDir);
            Log.Information("EnsureRepository: repo={repoId}, targetDirectory={targetDir}, RepoBaseDir={baseDir}, GitPath={gitPath}", repository, targetDirectory, _configuration.RepoBaseDir, _configuration.GitPath);
            var cloneWrapper = new GitWrapper(repoDir, _configuration.GitPath, _configuration.RepoBaseDir);
            var repoTarget = "https://" + authenticationToken + ":@" + repository.Substring(8);
            _progressLog.Info("Verifying {0}", repository);
            var lsRemoteResult = await cloneWrapper.ListRemote(repository, headsOnly: true, setExitCode: true);
            if (!lsRemoteResult.Success)
            {
                _progressLog.Warn("{0} appears to be an empty repository. Attempting to initialize it", repository);
                var gitWrapper = new GitWrapper(repoDir, _configuration.GitPath);
                var initResult = await gitWrapper.Init();
                if (initResult.Success)
                {
                    using (var writer = File.CreateText(Path.Combine(repoDir, "README.md")))
                    {
                        writer.WriteLine(
                            "Created by DataDock. You can delete this file after importing your first data set.");
                    }
                    var commitResult = await CommitChanges(repoDir, "Initial commit", userAccount);
                    if (commitResult)
                    {
                        var remoteResult = await gitWrapper.AddRemote("origin", repoTarget);
                        if (remoteResult.Success)
                        {
                            var pushed = await gitWrapper.Push("master");
                            //var pushed = await PushChanges(repository, repoDir, authenticationToken, true, branch:"master");
                            if (!pushed.Success) _progressLog.Error("Failed to push to new repository.");
                            RemoveDirectory(repoDir);
                            return pushed.Success;
                        }
                        else
                        {
                            _progressLog.Error("Failed to add origin remote");
                        }
                    }
                    else
                    {
                        _progressLog.Error("Failed to add initialization file to repository");
                    }
                }
                else
                {
                    _progressLog.Error("Failed to initialize local Git repository");
                }
                return false;
            }
            return true;
        }

        private async Task CloneRepository(string repository, string targetDirectory, string authenticationToken, UserAccount userAccount)
        {
            if (!await EnsureRepository(repository, targetDirectory, authenticationToken, userAccount))
            {
                throw new ConversionJobProcessorException("Failed to validate remote repository {0}. Please check that the repository exists and that you have write access to it.", repository);
            }

            var cloneWrapper = new GitWrapper(_configuration.RepoBaseDir, _configuration.GitPath);
            _progressLog.Info("Cloning {0}", repository);

            var cloneResult = await cloneWrapper.Clone(repository, targetDirectory, depth: 1, branch: "gh-pages");
            if (!cloneResult.Success)
            {
                LogError(cloneResult, $"Clone of repository {repository} gh-pages branch failed.");
                _progressLog.Info("Clone of gh-pages branch failed. Attempting to clone default branch and create a new gh-pages branch");

                cloneResult = await cloneWrapper.Clone(repository, targetDirectory, depth: 1);
                if (!cloneResult.Success)
                {
                    LogError(cloneResult, $"Clone of repository {repository} failed.");
                    throw new ConversionJobProcessorException("Clone of repository {0} failed.", repository);
                }
                var repoDir = Path.Combine(_configuration.RepoBaseDir, targetDirectory);
                var branchWrapper = new GitWrapper(repoDir, _configuration.GitPath);
                var branchResult = await branchWrapper.NewBranch("gh-pages", force: true);
                if (!branchResult.Success)
                {
                    LogError(cloneResult, $"Failed to create a new gh-pages branch in the repository {repository}.");
                    throw new ConversionJobProcessorException("Failed to create a gh-pages branch in the repository {0}", repository);
                }
                await PushChanges(repository, repoDir, authenticationToken, true);
            }
            _progressLog.Info("Clone of {0} complete", repository);
        }

        private void LogError(CommandResult commandResult, string errorMessage)
        {
            _progressLog.Error($"{errorMessage} Exit code was: {commandResult.ExitCode}. Command output follows.");
            _progressLog.Error("Clone command stdout: {0}", commandResult.StandardOutput);
            _progressLog.Error("Clone command stderr: {0}", commandResult.StandardError);
        }

        private async Task PushChanges(string remoteUrl, string repositoryDirectory, string authenticationToken, bool setUpstream = false, string branch = "gh-pages")
        {
            try
            {
                var gitWrapper = new GitWrapper(repositoryDirectory, _configuration.GitPath);
                var repoTarget = "https://" + authenticationToken + ":@" + remoteUrl.Substring(8);
                var pushResult = await gitWrapper.PushTo(repoTarget, branch, setUpstream);
                if (!pushResult.Success)
                {
                    _progressLog.Error("Failed to push to remote repository.");
                    throw new ConversionJobProcessorException("Failed to push to remote repository.");
                }
            }
            catch (ConversionJobProcessorException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _progressLog.Exception(ex, "Failed to push gh-pages branch to GitHub.");
                throw new ConversionJobProcessorException(ex, "Failed to push to remote repository.");
            }
        }

        private async Task<ReleaseInfo> MakeRelease(IGraph dataGraph, string releaseTag, string owner, string repositoryId, string datasetId, string repositoryDirectory, string authenticationToken)
        {
            var releaseInfo = new ReleaseInfo(releaseTag);
            var ntriplesDumpFileName = Path.Combine(repositoryDirectory, releaseTag + ".nt.gz");
            _progressLog.Info("Generating gzipped NTriples data dump");
            var writer = new GZippedNTriplesWriter();
            writer.Save(dataGraph, ntriplesDumpFileName);

            // Make a release
            try
            {
                _progressLog.Info("Generating a new release of dataset {0}", datasetId);
                if (authenticationToken == null) throw new ConversionJobProcessorException("No valid GitHub access token found for your account.");
                var client = _gitHubClientFactory.GetClient(authenticationToken);
                var releaseClient = client.Repository.Release;
                var newRelease = new NewRelease(releaseTag) { TargetCommitish = "gh-pages" };
                var release = await releaseClient.Create(owner, repositoryId, newRelease);

                // Attach data dump file(s) to release
                try
                {
                    _progressLog.Info("Uploading data dump files to GitHub release");
                    using (var zipFileStream = File.OpenRead(ntriplesDumpFileName))
                    {
                        var upload = new ReleaseAssetUpload(Path.GetFileName(ntriplesDumpFileName), "application/gzip",
                            zipFileStream, null);
                        var releaseAsset = await releaseClient.UploadAsset(release, upload);
                        releaseInfo.DownloadLinks.Add(releaseAsset.BrowserDownloadUrl);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to attach dump files to GitHub release");
                    throw new ConversionJobProcessorException(ex, "Failed to attach dump files to GitHub release");
                }
            }
            catch (ConversionJobProcessorException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to create a new GitHub release");
                throw new ConversionJobProcessorException(ex, "Failed to create a new GitHub release");
            }
            return releaseInfo;
        }

        private class ReleaseInfo
        {
            public string Tag { get; private set; }
            public List<string> DownloadLinks { get; private set; }

            public ReleaseInfo(string tag)
            {
                Tag = tag;
                DownloadLinks = new List<string>();
            }
        }

        private void AddReleaseMetadata(ReleaseInfo releaseInfo, IQuinceStore quinceStore, Uri datasetUri,
            Uri metadataGraphUri)
        {
            var g = new Graph();
            var dsNode = g.CreateUriNode(datasetUri);
            var ddNode = g.CreateUriNode(new Uri("http://rdfs.org/ns/void#dataDump"));
            foreach (var downloadLink in releaseInfo.DownloadLinks)
            {
                quinceStore.Assert(dsNode, ddNode, g.CreateUriNode(new Uri(downloadLink)), metadataGraphUri);
            }
            quinceStore.Flush();
        }

        private void RemoveDirectory(string directoryPath)
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

        public void Report(int value)
        {
            _progressLog.Info("CSV conversion processed {0} rows", value);
        }
    }

    public class ConversionJobProcessorConfiguration
    {
        /// <summary>
        /// The path to the Git executable
        /// </summary>
        public string GitPath { get; set; }

        /// <summary>
        /// The path to the directory to use for cloning user repositories
        /// </summary>
        public string RepoBaseDir { get; set; }

    }

    public class ConversionJobProcessorException : Exception
    {
        public ConversionJobProcessorException(string fmt, params object[] args) : base(string.Format(fmt, args)) { }
        public ConversionJobProcessorException(Exception innerException, string fmt, params object[] args) : base(string.Format(fmt, args), innerException) { }
    }
}
