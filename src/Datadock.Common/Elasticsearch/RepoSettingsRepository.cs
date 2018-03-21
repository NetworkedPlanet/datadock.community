using Datadock.Common.Models;
using Datadock.Common.Repositories;
using Nest;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Datadock.Common.Elasticsearch
{
    public class RepoSettingsRepository : IRepoSettingsRepository
    {
        private readonly IElasticClient _client;
        public RepoSettingsRepository(IElasticClient client, string indexName)
        {
            Log.Debug("Create RepoSettingsRepository. Index={indexName}", indexName);
            _client = client;
            // Ensure the index exists
            var indexExistsReponse = _client.IndexExists(indexName);
            if (!indexExistsReponse.Exists)
            {
                Log.Debug("Create ES index {indexName} for type {indexType}", indexName, typeof(JobInfo));
               var createIndexResponse =  _client.CreateIndex(indexName, config =>
                    config.Mappings(mappings => mappings.Map<RepoSettings>(m => m.AutoMap(-1))));
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
            throw new NotImplementedException();
        }

        public async Task CreateOrUpdateRepoSettingsAsync(RepoSettings settings)
        {
            throw new NotImplementedException();
        }
    }
}

