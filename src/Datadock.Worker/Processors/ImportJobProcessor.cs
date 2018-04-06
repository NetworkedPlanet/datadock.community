using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Datadock.Common.Models;
using Datadock.Common.Stores;
using DataDock.Common;
using DataDock.CsvWeb.Metadata;
using DataDock.CsvWeb.Parsing;
using DataDock.CsvWeb.Rdf;
using Newtonsoft.Json.Linq;
using Serilog;
using VDS.RDF;
using VDS.RDF.Parsing.Handlers;

namespace DataDock.Worker.Processors
{
    public class ImportJobProcessor : IDataDockProcessor, IProgress<int>
    {
        private readonly WorkerConfiguration _configuration;
        private readonly GitCommandProcessor _git;
        private readonly IDatasetStore _datasetStore;
        private readonly IOwnerSettingsStore _ownerSettingsStore;
        private readonly IRepoSettingsStore _repoSettingsStore;
        private readonly IFileStore _jobFileStore;
        private IProgressLog _progressLog;
        private readonly IDataDockRepository _dataDataDockRepository;
        private const int CsvConversionReportInterval = 250;

        public ImportJobProcessor(
            WorkerConfiguration configuration,
            GitCommandProcessor gitProcessor,
            IDatasetStore datasetStore,
            IFileStore jobFileStore,
            IOwnerSettingsStore ownerSettingsStore,
            IRepoSettingsStore repoSettingsStore,
            IDataDockRepository dataDockRepository)
        {
            _configuration = configuration;
            _git = gitProcessor;
            _datasetStore = datasetStore;
            _ownerSettingsStore = ownerSettingsStore;
            _repoSettingsStore = repoSettingsStore;
            _jobFileStore = jobFileStore;
            _dataDataDockRepository = dataDockRepository;
        }

        public async Task ProcessJob(JobInfo job, UserAccount userAccount, IProgressLog progressLog)
        {
            _progressLog = progressLog;
            var authenticationClaim =
                userAccount.Claims.FirstOrDefault(c => c.Type.Equals(DataDockClaimTypes.GitHubAccessToken));
            var authenticationToken = authenticationClaim?.Value;
            if (string.IsNullOrEmpty(authenticationToken))
            {
                Log.Error("No authentication token found for user {userId}", userAccount.UserId);
                _progressLog.Error("Could not find a valid GitHub access token for this user account. Please check your account settings.");
            }

            var targetDirectory = Path.Combine(_configuration.RepoBaseDir, job.JobId);
            Log.Information("Using local directory {localDirPath}", targetDirectory);

            // Clone the repository
            await _git.CloneRepository(job.GitRepositoryUrl, targetDirectory, authenticationToken, userAccount);

            // Retrieve CSV and CSVM files to src directory in the repository
            await AddCsvFilesToRepository(targetDirectory,
                job.DatasetId,
                job.CsvFileName,
                job.CsvFileId,
                job.CsvmFileId);

            var csvPath = Path.Combine(targetDirectory, "csv", job.DatasetId, job.CsvFileName);
            var metaPath = Path.Combine(targetDirectory, "csv", job.DatasetId, job.CsvFileName + "-metadata.json");

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
                new[] { ntriplesDownloadLink, csvDownloadLink }, datasetGraph);

            IGraph definitionsGraph = GenerateDefinitionsGraph(metadataJson);

            _dataDataDockRepository.UpdateDataset(
                datasetGraph, datasetUri, job.OverwriteExistingData,
                metadataGraph, datasetMetadataGraphIri, 
                definitionsGraph, definitionsGraphIri, 
                publisherIri, publisher,
                "", "",
                rootMetadataGraphIri);

            _dataDataDockRepository.Publish(
                new[] { datasetUri, datasetMetadataGraphIri, rootMetadataGraphIri });

            // Add and Commit all changes
            if (await _git.CommitChanges(targetDirectory,
                $"Added {job.CsvFileName} to dataset {job.DatasetIri}", userAccount))
            {
                await _git.PushChanges(job.GitRepositoryUrl, targetDirectory, authenticationToken);
                await _git.MakeRelease(datasetGraph, releaseTag, job.OwnerId, job.RepositoryId,
                    job.DatasetId, targetDirectory, authenticationToken);
            }

            // Update the dataset repository
            try
            {
                await _datasetStore.CreateOrUpdateDatasetRecordAsync(new DatasetInfo
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
                throw new WorkerException(ex,
                    "Failed to update dataset record. Your repository is updated, but may not show in the main lodlab portal.");
            }

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
                throw new WorkerException(ex, "Failed to copy CSV/CSVM files from upload to Github repository.");
            }
        }

        private static string MakeSafeTag(string tag)
        {
            return Regex.Replace(tag, @"[^a-zA-Z0-9]", "_", RegexOptions.None);
        }

        private async Task<ContactInfo> GetPublisherContactInfo(string ownerId, string repoId)
        {
            try
            {
                _progressLog.Info("Attempting to retrieve publisher contact information from repository settings");
                // get repoSettings
                var repoSettings = await _repoSettingsStore.GetRepoSettingsAsync(ownerId + "/" + repoId);
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
                    var ownerSettings = await _ownerSettingsStore.GetOwnerSettingsAsync(ownerId);
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

        private Graph GenerateDatasetGraph(TextReader csvReader, JObject metadataJson)
        {
            var parser = new JsonMetadataParser(null);
            Table tableMeta;
            try
            {
                tableMeta = parser.Parse(metadataJson) as Table;
                if (tableMeta == null)
                {
                    throw new WorkerException("CSV Conversion failed. Unable to read CSV table metadata.");
                }
            }
            catch (MetadataParseException ex)
            {
                Log.Error(ex, "Unable to parse CSV table metadata");
                throw new WorkerException(ex, "CSV conversion failed. Unable to parse CSV table metadata.");
            }

            var graph = new Graph();
            _progressLog.Info("Running CSV to RDF conversion");
            var graphHandler = new GraphHandler(graph);
            var converter = new Converter(tableMeta, graphHandler, (msg) => _progressLog.Error(msg), this, reportInterval: CsvConversionReportInterval);
            converter.Convert(csvReader);
            if (converter.Errors.Any())
            {
                throw new WorkerException("One or more errors where encountered during the CSV to RDF conversion.");
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


        public void Report(int value)
        {
            _progressLog.Info("CSV conversion processed {0} rows", value);
        }
    }


}
