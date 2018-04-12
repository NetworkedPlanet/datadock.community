using Datadock.Common.Models;
using Nest;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        public async Task<IEnumerable<RepoSettings>> GetRepoSettingsForOwnerAsync(string ownerId)
        {
            if (ownerId == null) throw new ArgumentNullException(nameof(ownerId));
            
            var search = new SearchDescriptor<RepoSettings>().Query(q => QueryByOwnerId(q, ownerId));
            var rawQuery = "";
            using (var ms = new MemoryStream())
            {
                _client.RequestResponseSerializer.Serialize(search, ms);
                rawQuery = Encoding.UTF8.GetString(ms.ToArray());
                Console.WriteLine(rawQuery);
            }
            var response =
                await _client.SearchAsync<RepoSettings>(search);

            if (!response.IsValid)
            {
                throw new RepoSettingsStoreException(
                    $"Error retrieving repository settings for owner {ownerId}. Cause: {response.DebugInformation}");
            }

            if (response.Total < 1)
            {
                Log.Warning($"No settings found with query {rawQuery}");
                throw new RepoSettingsNotFoundException(ownerId);
            }
            return response.Documents;
        }

        public async Task<RepoSettings> GetRepoSettingsAsync(string ownerId, string repoId)
        {
            if (ownerId == null) throw new ArgumentNullException(nameof(ownerId));
            if (repoId == null) throw new ArgumentNullException(nameof(repoId));
            var rawQuery = "";
            var search = new SearchDescriptor<RepoSettings>().Query(q => QueryByOwnerIdAndRepositoryId(q, ownerId, repoId));
            using (var ms = new MemoryStream())
            {
                _client.RequestResponseSerializer.Serialize(search, ms);
                rawQuery = Encoding.UTF8.GetString(ms.ToArray());
                Console.WriteLine(rawQuery);
            }

            var response =
                await _client.SearchAsync<RepoSettings>(search);
            
            if (!response.IsValid)
            {
                throw new RepoSettingsStoreException(
                    $"Error retrieving repository settings for repo ID {repoId} on owner {ownerId}. Cause: {response.DebugInformation}");
            }

            if (response.Total < 1)
            {
                Log.Warning($"No settings found with query {rawQuery}");
                throw new RepoSettingsNotFoundException(ownerId, repoId);
            }
            return response.Documents.FirstOrDefault();
        }

        public async Task CreateOrUpdateRepoSettingsAsync(RepoSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (string.IsNullOrEmpty(settings.FullId))
            {
                settings.FullId = $"{settings.OwnerId}/{settings.RepoId}";
            }
            var validator = new RepoSettingsValidator();
            var validationResults = await validator.ValidateAsync(settings);
            if (!validationResults.IsValid)
            {
                throw new ValidationException("Invalid repo settings", validationResults);
            }
            var updateResponse = await _client.IndexDocumentAsync(settings);
            if (!updateResponse.IsValid)
            {
                throw new OwnerSettingsStoreException($"Error updating repo settings for owner/repo ID {settings.RepoId}");
            }
        }

        private static QueryContainer QueryByOwnerId(QueryContainerDescriptor<RepoSettings> q, string ownerId)
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

        private static QueryContainer QueryByOwnerIdAndRepositoryId(QueryContainerDescriptor<RepoSettings> q, string ownerId, string repoId)
        {
            var filterClauses = new List<QueryContainer>
            {
                new TermQuery
                {
                    Field = new Field("ownerId"),
                    Value = ownerId
                },
                new TermQuery
                {
                    Field = new Field("repoId"),
                    Value = repoId
                }
            };
            return new BoolQuery { Filter = filterClauses };
        }
    }
}

