﻿using Datadock.Common.Models;
using Datadock.Common.Stores;
using DataDock.Common;
using Nest;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datadock.Common.Elasticsearch
{
    public class DatasetStore : IDatasetStore
    {
        private readonly IElasticClient _client;

        public DatasetStore(IElasticClient client, ApplicationConfiguration config)
        {
            var indexName = config.DatasetIndexName;
            Log.Debug("Create DatasetStore. Index={indexName}", indexName);
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
                        $"Could not create index {indexName} for DatasetStore. Cause: {createIndexResponse.DebugInformation}");
                }
            }
            _client.ConnectionSettings.DefaultIndices[typeof(DatasetInfo)] = indexName;
        }

        public async Task<IEnumerable<DatasetInfo>> GetRecentlyUpdatedDatasetsAsync(int limit, bool showHidden = false)
        {
            var search = new SearchDescriptor<DatasetInfo>();
            if (!showHidden)
            {
                // add query to filter by only those datasets with showOnHomepage = true
                search.Query(q => QueryHelper.FilterByShowOnHomepage());
            }
            var rawQuery = "";
            using (var ms = new MemoryStream())
            {
                _client.RequestResponseSerializer.Serialize(search, ms);
                rawQuery = Encoding.UTF8.GetString(ms.ToArray());
                Console.WriteLine(rawQuery);
            }
            var searchResponse =
                await _client.SearchAsync<DatasetInfo>(search
                    .Skip(0)
                    .Take(limit)
                    .Sort(s => s.Field(f => f.Field("lastModified").Order(SortOrder.Descending))));

            if (!searchResponse.IsValid)
            {
                throw new DatasetStoreException(
                    $"Error retrieving datasets. Cause: {searchResponse.DebugInformation}");
            }
            if (searchResponse.Total < 1) throw new DatasetNotFoundException("all");
            return searchResponse.Documents;
        }

        public async Task<IEnumerable<DatasetInfo>> GetRecentlyUpdatedDatasetsForOwnersAsync(string[] ownerIds, int skip, int take, bool showHidden = false)
        {
           
            var search = new SearchDescriptor<DatasetInfo>().Query(q => QueryHelper.FilterByOwnerIds(ownerIds, showHidden));
            var rawQuery = "";
            using (var ms = new MemoryStream())
            {
                _client.RequestResponseSerializer.Serialize(search, ms);
                rawQuery = Encoding.UTF8.GetString(ms.ToArray());
                Console.WriteLine(rawQuery);
            }
            var searchResponse =
                await _client.SearchAsync<DatasetInfo>(search
                    .Skip(skip)
                    .Take(take)
                    .Sort(s => s.Field(f => f.Field("lastModified").Order(SortOrder.Descending))));

            var ownerIdsString = string.Join(", ", ownerIds);
            if (!searchResponse.IsValid)
            {
                throw new DatasetStoreException(
                    $"Error retrieving datasets for owner IDs {ownerIdsString}. Cause: {searchResponse.DebugInformation}");
            }
            if (searchResponse.Total < 1) throw new DatasetNotFoundException(ownerIdsString);
            return searchResponse.Documents;
        }

        public async Task<IEnumerable<DatasetInfo>> GetRecentlyUpdatedDatasetsForRepositoriesAsync(string ownerId, string[] repositoryIds, int skip, int take, bool showHidden = false)
        {
            if (string.IsNullOrEmpty(ownerId)) throw new ArgumentNullException(nameof(ownerId));
            var search = new SearchDescriptor<DatasetInfo>().Query(q => QueryHelper.FilterByOwnerIdAndRepositoryIds(ownerId, repositoryIds, showHidden));
            var rawQuery = "";
            using (var ms = new MemoryStream())
            {
                _client.RequestResponseSerializer.Serialize(search, ms);
                rawQuery = Encoding.UTF8.GetString(ms.ToArray());
                Console.WriteLine(rawQuery);
            }
            var searchResponse =
                await _client.SearchAsync<DatasetInfo>(search
                    .Skip(skip)
                    .Take(take)
                    .Sort(s => s.Field(f => f.Field("lastModified").Order(SortOrder.Descending))));

            var repositoryIdsString = string.Join(", ", repositoryIds);
            if (!searchResponse.IsValid)
            {
                throw new DatasetStoreException(
                    $"Error retrieving datasets for repo IDs {repositoryIdsString} on owner {ownerId}. Cause: {searchResponse.DebugInformation}");
            }
            if (searchResponse.Total < 1) throw new DatasetNotFoundException(ownerId, repositoryIdsString);
            return searchResponse.Documents;

        }

        public async Task<IEnumerable<DatasetInfo>> GetDatasetsForOwnerAsync(string ownerId, int skip, int take)
        {
            if (string.IsNullOrEmpty(ownerId)) throw new ArgumentNullException(nameof(ownerId));

            var search = new SearchDescriptor<DatasetInfo>().Query(q => QueryHelper.FilterByOwnerId(ownerId));
            var rawQuery = "";
            using (var ms = new MemoryStream())
            {
                _client.RequestResponseSerializer.Serialize(search, ms);
                rawQuery = Encoding.UTF8.GetString(ms.ToArray());
                Console.WriteLine(rawQuery);
            }
            var response =
                await _client.SearchAsync<DatasetInfo>(search.Skip(skip).Take(take));
            
            if (!response.IsValid)
            {
                throw new DatasetStoreException(
                    $"Error retrieving datasets for owner {ownerId}. Cause: {response.DebugInformation}");
            }
            if (response.Total < 1) throw new DatasetNotFoundException(ownerId);
            return response.Documents;
        }

        public async Task<IEnumerable<DatasetInfo>> GetDatasetsForRepositoryAsync(string ownerId, string repositoryId, int skip, int take)
        {
            if (string.IsNullOrEmpty(ownerId)) throw new ArgumentNullException(nameof(ownerId));
            if (string.IsNullOrEmpty(repositoryId)) throw new ArgumentNullException(nameof(repositoryId));

            var rawQuery = "";
            var search = new SearchDescriptor<DatasetInfo>().Query(q => QueryHelper.FilterByOwnerIdAndRepositoryId(ownerId, repositoryId));
            using (var ms = new MemoryStream())
            {
                _client.RequestResponseSerializer.Serialize(search, ms);
                rawQuery = Encoding.UTF8.GetString(ms.ToArray());
                Console.WriteLine(rawQuery);
            }
            var response = await _client.SearchAsync<DatasetInfo>(search.Skip(skip).Take(take));
            
            if (!response.IsValid)
            {
                throw new DatasetStoreException(
                    $"Error retrieving datasets for repo ID {repositoryId} on owner {ownerId}. Query: {rawQuery} Cause: {response.DebugInformation}");
            }

            if (response.Total < 1)
            {
                Log.Information($"No datasets found with query {rawQuery}");
                throw new DatasetNotFoundException(ownerId, repositoryId);
            }
            return response.Documents;
        }
        
        public async Task<DatasetInfo> GetDatasetInfoAsync(string ownerId, string repositoryId, string datasetId)
        {
            if (string.IsNullOrEmpty(ownerId)) throw new ArgumentNullException(nameof(ownerId));
            if (string.IsNullOrEmpty(repositoryId)) throw new ArgumentNullException(nameof(repositoryId));
            if (string.IsNullOrEmpty(datasetId)) throw new ArgumentNullException(nameof(datasetId));

            var rawQuery = "";
            var search = new SearchDescriptor<DatasetInfo>().Query(q => QueryHelper.FilterByOwnerIdAndRepositoryIdAndDatasetId(ownerId, repositoryId, datasetId));
            using (var ms = new MemoryStream())
            {
                _client.RequestResponseSerializer.Serialize(search, ms);
                rawQuery = Encoding.UTF8.GetString(ms.ToArray());
                Console.WriteLine(rawQuery);
            }

            var response =
                await _client.SearchAsync<DatasetInfo>(search);

            if (!response.IsValid)
            {
                throw new DatasetStoreException(
                    $"Error retrieving dataset for dataset ID {datasetId}, repo ID {repositoryId} on owner {ownerId}. Cause: {response.DebugInformation}");
            }

            if (response.Total < 1)
            {
                Log.Warning($"No settings found with query {rawQuery}");
                throw new DatasetNotFoundException(ownerId, repositoryId, datasetId);
            }
            return response.Documents.FirstOrDefault();
        }

        public async Task<DatasetInfo> GetDatasetInfoByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            try
            {
                var datasetInfo = await _client.GetAsync<DatasetInfo>(new DocumentPath<DatasetInfo>(id));
                return datasetInfo.Source;
            }
            catch (Exception e)
            {
                throw new DatasetStoreException(
                    $"Error retrieving dataset by ID {id}. Cause: {e.ToString()}");
            }
        }

        public async Task<IEnumerable<DatasetInfo>> GetDatasetsForTagAsync(string tag)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<DatasetInfo>> GetDatasetsForTagAsync(string ownerId, string[] tags, bool matchAll = false, bool showHidden = false)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<DatasetInfo>> GetDatasetsForTagAsync(string ownerId, string repositoryId, string[] tags, bool matchAll = false,
            bool showHidden = false)
        {
            throw new NotImplementedException();
        }

        public async Task<DatasetInfo> CreateOrUpdateDatasetRecordAsync(DatasetInfo datasetInfo)
        {
            if (datasetInfo == null) throw new ArgumentNullException();
            if (string.IsNullOrEmpty(datasetInfo.Id))
            {
                datasetInfo.Id = $"{datasetInfo.OwnerId}/{datasetInfo.RepositoryId}/{datasetInfo.DatasetId}";
            }
            var indexResponse =await _client.IndexDocumentAsync(datasetInfo);
            if (!indexResponse.IsValid)
            {
                throw new DatasetStoreException(
                    $"Failed to index dataset record. Cause: {indexResponse.DebugInformation}");
            }
            return datasetInfo;
        }

        public async Task<bool> DeleteDatasetsForOwnerAsync(string ownerId)
        {
            var deleteResponse = await _client.DeleteByQueryAsync<DatasetInfo>(s => s.Query(q => QueryByOwnerId(q, ownerId)));
            if (!deleteResponse.IsValid)
            {
                throw new DatasetStoreException(
                    $"Failed to delete all datasets owned by {ownerId}");
            }
            return true;
        }

        public Task DeleteDatasetAsync(string ownerId, string repositoryId, string datasetId)
        {
            throw new NotImplementedException();
        }

        private static QueryContainer QueryByOwnerId(QueryContainerDescriptor<DatasetInfo> q, string ownerId)
        {
            var filterClauses = new List<QueryContainer>
            {
                new TermQuery
                {
                    Field = new Field("ownerId"),
                    Value = ownerId
                }
            };
            return new BoolQuery { Filter = filterClauses };
        }
    }
}
