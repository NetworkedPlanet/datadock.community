using Datadock.Common.Models;
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

        public async Task<IEnumerable<DatasetInfo>> GetRecentlyUpdatedDatasets(int limit, bool showHidden = false)
        {
            var mustClauses = new List<QueryContainer>
            {
                new TermQuery
                {
                    Field = new Field("showOnHomepage"),
                    Value = showHidden
                }
            };
            var searchRequest = new SearchRequest<DatasetInfo>
            {
                Size = limit,
                From = 0,
                Query = new BoolQuery { Must = mustClauses }
            };

            var searchResponse = await _client.SearchAsync<DatasetInfo>(searchRequest);

            if (!searchResponse.IsValid)
            {
                throw new DatasetStoreException(
                    $"Error retrieving datasets. Cause: {searchResponse.DebugInformation}");
            }
            if (searchResponse.Total < 1) throw new DatasetNotFoundException("all");
            return searchResponse.Documents;
        }

        public async Task<IEnumerable<DatasetInfo>> GetRecentlyUpdatedDatasetsForOwners(string[] ownerIds, int skip, int take, bool showHidden = false)
        {
            var mustClauses = new List<QueryContainer>
            {
                new TermQuery
                {
                    Field = new Field("showOnHomepage"),
                    Value = showHidden
                }
            };
            var shouldClauses = new List<QueryContainer>();
            foreach (var repositoryId in ownerIds)
            {
                shouldClauses.Add(new TermQuery { Field = new Field("ownerIds"), Value = repositoryId });
            }
            var searchRequest = new SearchRequest<DatasetInfo>
            {
                Size = take,
                From = skip,
                Query = new BoolQuery { Must = mustClauses, Should = shouldClauses }
            };

            var searchResponse = await _client.SearchAsync<DatasetInfo>(searchRequest);

            var ownerIdsString = string.Join(", ", ownerIds);
            if (!searchResponse.IsValid)
            {
                throw new DatasetStoreException(
                    $"Error retrieving datasets for owner IDs {ownerIdsString}. Cause: {searchResponse.DebugInformation}");
            }
            if (searchResponse.Total < 1) throw new DatasetNotFoundException(ownerIdsString);
            return searchResponse.Documents;
        }

        public async Task<IEnumerable<DatasetInfo>> GetRecentlyUpdatedDatasetsForRepositories(string ownerId, string[] repositoryIds, int skip, int take, bool showHidden = false)
        {
            var mustClauses = new List<QueryContainer>
            {
                new TermQuery
                {
                    Field = new Field("ownerId"),
                    Value = ownerId
                },
                new TermQuery
                {
                    Field = new Field("showOnHomepage"),
                    Value = showHidden
                }
            };
            var shouldClauses = new List<QueryContainer>();
            foreach (var repositoryId in repositoryIds)
            {
                shouldClauses.Add(new TermQuery{Field = new Field("repositoryId"), Value = repositoryId});
            }
            var searchRequest = new SearchRequest<DatasetInfo>
            {
                Size = take,
                From = skip,
                Query = new BoolQuery { Must = mustClauses, Should = shouldClauses }
            };

            var searchResponse = await _client.SearchAsync<DatasetInfo>(searchRequest);

            var repositoryIdsString = string.Join(", ", repositoryIds);
            if (!searchResponse.IsValid)
            {
                throw new DatasetStoreException(
                    $"Error retrieving datasets for repo IDs {repositoryIdsString} on owner {ownerId}. Cause: {searchResponse.DebugInformation}");
            }
            if (searchResponse.Total < 1) throw new DatasetNotFoundException(ownerId, repositoryIdsString);
            return searchResponse.Documents;

        }

        public async Task<IEnumerable<DatasetInfo>> GetDatasetsForOwner(string ownerId, int skip, int take)
        {
            if (string.IsNullOrEmpty(ownerId)) throw new ArgumentNullException(nameof(ownerId));

            var response = await _client.SearchAsync<DatasetInfo>(s => s
                .From(0).Query(q => q.Match(m => m.Field(f => f.OwnerId).Query(ownerId)))
            );
            if (!response.IsValid)
            {
                throw new DatasetStoreException(
                    $"Error retrieving datasets for owner {ownerId}. Cause: {response.DebugInformation}");
            }
            if (response.Total < 1) throw new DatasetNotFoundException(ownerId);
            return response.Documents;
        }

        public async Task<IEnumerable<DatasetInfo>> GetDatasetsForRepository(string ownerId, string repositoryId, int skip, int take)
        {
            if (string.IsNullOrEmpty(ownerId)) throw new ArgumentNullException(nameof(ownerId));
            if (string.IsNullOrEmpty(repositoryId)) throw new ArgumentNullException(nameof(repositoryId));

            var rawQuery = "";
            var search = new SearchDescriptor<DatasetInfo>().Query(q => QueryHelper.QueryByOwnerIdAndRepositoryId(ownerId, repositoryId));
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
        
        public async Task<DatasetInfo> GetDatasetInfo(string ownerId, string repositoryId, string datasetId)
        {
            if (string.IsNullOrEmpty(ownerId)) throw new ArgumentNullException(nameof(ownerId));
            if (string.IsNullOrEmpty(repositoryId)) throw new ArgumentNullException(nameof(repositoryId));
            if (string.IsNullOrEmpty(datasetId)) throw new ArgumentNullException(nameof(datasetId));

            var mustClauses = new List<QueryContainer>
            {
                new TermQuery
                {
                    Field = new Field("ownerId"),
                    Value = ownerId
                },
                new TermQuery
                {
                    Field = new Field("repositoryId"),
                    Value = repositoryId
                },
                new TermQuery
                {
                    Field = new Field("datasetId"),
                    Value = datasetId
                }
            };
           
            var searchRequest = new SearchRequest<DatasetInfo>
            {
                Query = new BoolQuery { Must = mustClauses}
            };

            var searchResponse = await _client.SearchAsync<DatasetInfo>(searchRequest);

            if (!searchResponse.IsValid)
            {
                throw new DatasetStoreException(
                    $"Error retrieving dataset for owner ID {ownerId} repositoryId {repositoryId} datasetId {datasetId}. Cause: {searchResponse.DebugInformation}");
            }
            if (searchResponse.Total < 1) throw new DatasetNotFoundException(ownerId, repositoryId, datasetId);
            return searchResponse.Documents.FirstOrDefault();
        }

        public async Task<IEnumerable<DatasetInfo>> GetDatasetsForTag(string tag)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<DatasetInfo>> GetDatasetsForTag(string ownerId, string[] tags, bool matchAll = false, bool showHidden = false)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<DatasetInfo>> GetDatasetsForTag(string ownerId, string repositoryId, string[] tags, bool matchAll = false,
            bool showHidden = false)
        {
            throw new NotImplementedException();
        }

        public async Task<DatasetInfo> CreateOrUpdateDatasetRecordAsync(DatasetInfo datasetInfo)
        {
            if (datasetInfo == null) throw new ArgumentNullException();
            if (string.IsNullOrEmpty(datasetInfo.Id))
            {
                datasetInfo.Id = $"{datasetInfo.OwnerId}.{datasetInfo.RepositoryId}.{datasetInfo.DatasetId}";
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
