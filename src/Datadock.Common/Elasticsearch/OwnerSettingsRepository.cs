using Datadock.Common.Models;
using Datadock.Common.Repositories;
using Nest;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Datadock.Common.Elasticsearch
{
    public class OwnerSettingsRepository : IOwnerSettingsRepository
    {
        private readonly IElasticClient _client;
        public OwnerSettingsRepository(IElasticClient client, string indexName)
        {
            Log.Debug("Create OwnerSettingsRepository. Index={indexName}", indexName);
            _client = client;
            // Ensure the index exists
            var indexExistsReponse = _client.IndexExists(indexName);
            if (!indexExistsReponse.Exists)
            {
                Log.Debug("Create ES index {indexName} for type {indexType}", indexName, typeof(JobInfo));
               var createIndexResponse =  _client.CreateIndex(indexName, config =>
                    config.Mappings(mappings => mappings.Map<JobInfo>(m => m.AutoMap(-1))));
                if (!createIndexResponse.Acknowledged)
                {
                    Log.Error("Create ES index failed for {indexName}. Cause: {detail}", indexName, createIndexResponse.DebugInformation);
                    throw new DatadockException(
                        $"Could not create index {indexName} for owner settings repository. Cause: {createIndexResponse.DebugInformation}");
                }
            }
        }

        public async Task<OwnerSettings> GetOwnerSettingsAsync(string ownerId)
        {
            throw new NotImplementedException();
        }

        public async Task CreateOrUpdateOwnerSettingsAsync(OwnerSettings ownerSettings)
        {
            throw new NotImplementedException();
        }
    }
}

