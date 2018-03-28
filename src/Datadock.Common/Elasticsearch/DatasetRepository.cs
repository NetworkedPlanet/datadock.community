using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Datadock.Common.Models;
using Datadock.Common.Stores;
using DataDock.Common;
using Nest;
using Serilog;

namespace Datadock.Common.Elasticsearch
{
    public class DatasetRepository : IDatasetRepository
    {
        private readonly IElasticClient _client;

        public DatasetRepository(IElasticClient client, ApplicationConfiguration config)
        {
            var indexName = config.DatasetIndexName;
            Log.Debug("Create DatasetRepository. Index={indexName}", indexName);
            _client = client;
            // Ensure the index exists
            var indexExistsReponse = _client.IndexExists(indexName);
            if (!indexExistsReponse.Exists)
            {
                Log.Debug("Create ES index {indexName} for type {indexType}", indexName, typeof(DatasetInfo));
                var createIndexResponse = _client.CreateIndex(indexName, c => c.Mappings(
                    mappings => mappings.Map<DatasetInfo>(m => m.AutoMap(-1))));
                if (!createIndexResponse.Acknowledged)
                {
                    Log.Error("Create ES index failed for {indexName}. Cause: {detail}", indexName, createIndexResponse.DebugInformation);
                    throw new DatadockException(
                        $"Could not create index {indexName} for Job repository. Cause: {createIndexResponse.DebugInformation}");
                }
            }
            _client.ConnectionSettings.DefaultIndices[typeof(DatasetInfo)] = indexName;
        }

        public IReadOnlyList<DatasetInfo> GetRecentlyUpdatedDatasets(int limit, bool showHidden = false)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<DatasetInfo> GetRecentlyUpdatedDatasets(string[] ownerIds, int skip, int take, bool showHidden = false)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<DatasetInfo> GetRecentlyUpdatedRepositoryDatasets(string[] repositoryIds, int skip, int take, bool showHidden = false)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<DatasetInfo> GetDatasetsForRepository(string repositoryId, int skip, int take)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<DatasetInfo> GetDatasetsForOwner(string ownerId, int skip, int take)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<string> GetRepositoryIdListForOwner(string[] ownerIds)
        {
            throw new NotImplementedException();
        }

        public DatasetInfo GetDatasetInfo(string repositoryId, string datasetId)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<DatasetInfo> GetDatasetsForTag(string tag)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<DatasetInfo> GetDatasetsForTag(string ownerId, string[] tags, bool matchAll = false, bool showHidden = false)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<DatasetInfo> GetDatasetsForTag(string ownerId, string repositoryId, string[] tags, bool matchAll = false,
            bool showHidden = false)
        {
            throw new NotImplementedException();
        }

        public async Task CreateOrUpdateDatasetRecordAsync(DatasetInfo datasetInfo)
        {
            var indexResponse =await _client.IndexDocumentAsync(datasetInfo);
            if (!indexResponse.IsValid)
            {
                throw new DatasetRepositoryException(
                    $"Failed to index dataset record. Cause: {indexResponse.DebugInformation}");
            }
        }

        public Task<bool> DeleteDatasetsForOwnerAsync(string ownerId)
        {
            throw new NotImplementedException();
        }

        public Task DeleteDatasetAsync(string repositoryId, string datasetId)
        {
            throw new NotImplementedException();
        }
    }
}
