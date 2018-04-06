using Datadock.Common.Models;
using Nest;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;
using Datadock.Common.Stores;
using Datadock.Common.Validators;
using DataDock.Common;

namespace Datadock.Common.Elasticsearch
{
    public class RepoSettingsStore : IRepoSettingsStore
    {
        private readonly IElasticClient _client;
        public RepoSettingsStore(IElasticClient client, ApplicationConfiguration config)
        {
            var indexName = config.RepoSettingsIndexName;
            Log.Debug("Create RepoSettingsStore. Index={indexName}", indexName);
            _client = client;
            // Ensure the index exists
            var indexExistsReponse = _client.IndexExists(indexName);
            if (!indexExistsReponse.Exists)
            {
                Log.Debug("Create ES index {indexName} for type {indexType}", indexName, typeof(JobInfo));
                var createIndexResponse = _client.CreateIndex(indexName,
                    c => c.Mappings(mappings => mappings.Map<RepoSettings>(m => m.AutoMap(-1))));
                if (!createIndexResponse.Acknowledged)
                {
                    Log.Error("Create ES index failed for {indexName}. Cause: {detail}", indexName, createIndexResponse.DebugInformation);
                    throw new DatadockException(
                        $"Could not create index {indexName} for repo settings repository. Cause: {createIndexResponse.DebugInformation}");
                }
            }

            _client.ConnectionSettings.DefaultIndices[typeof(RepoSettings)] = indexName;
        }

        public async Task<RepoSettings> GetRepoSettingsAsync(string ownerId, string repoId)
        {           
            var response = await _client.SearchAsync<RepoSettings>(s => s
                .From(0).Query(q => q.Match(m => m.Field(f => f.OwnerId).Query(ownerId)) &&
                                    q.Match(m => m.Field(f => f.RepositoryId).Query(repoId)))
            );
            if (!response.IsValid)
            {
                throw new RepoSettingsStoreException(
                    $"Error retrieving repository settings for repo ID {repoId} on owner {ownerId}. Cause: {response.DebugInformation}");
            }
            if (response.Total < 1) throw new RepoSettingsNotFoundException($"{ownerId}/{repoId}");
            return response.Documents.FirstOrDefault();
        }

        public async Task CreateOrUpdateRepoSettingsAsync(RepoSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            var validator = new RepoSettingsValidator();
            var validationResults = await validator.ValidateAsync(settings);
            if (!validationResults.IsValid)
            {
                throw new ValidationException("Invalid repo settings", validationResults);
            }
            var updateResponse = await _client.IndexDocumentAsync(settings);
            if (!updateResponse.IsValid)
            {
                throw new OwnerSettingsStoreException($"Error updating repo settings for owner/repo ID {settings.RepositoryId}");
            }
        }
    }
}

