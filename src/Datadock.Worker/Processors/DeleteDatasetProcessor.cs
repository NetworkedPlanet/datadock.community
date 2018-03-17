using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Datadock.Common.Models;
using Datadock.Common.Repositories;
using DataDock.Common;
using Serilog;

namespace DataDock.Worker.Processors
{
    public class DeleteDatasetProcessor : IDataDockProcessor
    {
        private readonly IProgressLog _progressLog;
        private readonly WorkerConfiguration _configuration;
        private readonly GitCommandProcessor _git;
        private readonly IDatasetRepository _datasetRepository;
        private readonly IQuinceStoreFactory _quinceStoreFactory;
        private readonly IHtmlGeneratorFactory _htmlGeneratorFactory;

        public DeleteDatasetProcessor(WorkerConfiguration configuration,
            IProgressLog progressLog, GitCommandProcessor gitProcessor,
            IDatasetRepository datasetRepository,
            IQuinceStoreFactory quinceStoreFactory,
            IHtmlGeneratorFactory htmlGeneratorFactory)
        {
            _configuration = configuration;
            _progressLog = progressLog;
            _git = gitProcessor;
            _datasetRepository = datasetRepository;
            _quinceStoreFactory = quinceStoreFactory;
            _htmlGeneratorFactory = htmlGeneratorFactory;
        }

        public async Task ProcessJob(JobInfo jobInfo, UserAccount userAccount)
        {
            var authenticationClaim =
                userAccount.Claims.FirstOrDefault(c => c.Type.Equals(DataDockClaimTypes.GitHubAccessToken));
            var authenticationToken = authenticationClaim?.Value;
            if (string.IsNullOrEmpty(authenticationToken))
            {
                Log.Error("No authentication token found for user {userId}", userAccount.UserId);
                _progressLog.Error("Could not find a valid GitHub access token for this user account. Please check your account settings.");
            }

            var targetDirectory = Path.Combine(_configuration.RepoBaseDir, jobInfo.JobId);
            Log.Information("Using local directory {localDirPath}", targetDirectory);

            Log.Information("Clone Repository: {gitRepositoryUrl} => {targetDirectory}", jobInfo.GitRepositoryUrl, targetDirectory);
            await _git.CloneRepository(jobInfo.GitRepositoryUrl, targetDirectory, authenticationToken, userAccount);

            var repositoryIri = new Uri(DataDockUrlHelper.GetRepositoryUri(jobInfo.GitRepositoryFullName));
            var datasetIri = new Uri(jobInfo.DatasetIri);
            var ddRepository = new DataDockRepository(targetDirectory, repositoryIri, _progressLog, _quinceStoreFactory, _htmlGeneratorFactory);

            DeleteCsvAndMetadata(targetDirectory, jobInfo.DatasetId);
            ddRepository.DeleteDataset(datasetIri);
            ddRepository.Publish();

            if (await _git.CommitChanges(targetDirectory,
                $"Deleted dataset {datasetIri}", userAccount))
            {
                await _git.PushChanges(jobInfo.GitRepositoryUrl, targetDirectory, authenticationToken);
            }
            try
            {
                await _datasetRepository.DeleteDatasetAsync(jobInfo.GitRepositoryFullName, jobInfo.DatasetId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to remove dataset record.");
                throw new WorkerException(ex, "Failed to remove dataset record. Your repository is updated but the dataset may still show in the main lodlab portal");
            }

        }


        private void DeleteCsvAndMetadata(string baseDirectory, string datasetId)
        {
            Log.Information("DeleteCsvAndMetadata: {baseDirectory}, {datasetId}", baseDirectory, datasetId);
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

    }
}
