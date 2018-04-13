using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datadock.Common.Models;
using Datadock.Common.Stores;
using DataDock.Common;
using Nest;
using Serilog;

namespace Datadock.Common.Elasticsearch
{
    public class SchemaStore : ISchemaStore
    {
        private readonly IElasticClient _client;
        public SchemaStore(IElasticClient client, ApplicationConfiguration config)
        {
            var indexName = config.SchemaIndexName;
            Log.Debug("Create SchemaStore. Index={indexName}", indexName);
            _client = client;
            // Ensure the index exists
            var indexExistsReponse = _client.IndexExists(indexName);
            if (!indexExistsReponse.Exists)
            {
                Log.Debug("Create ES index {indexName} for type {indexType}", indexName, typeof(SchemaInfo));
                var createIndexResponse = _client.CreateIndex(indexName,
                    c => c.Mappings(mappings => mappings.Map<SchemaInfo>(m => m.AutoMap(-1))));
                if (!createIndexResponse.Acknowledged)
                {
                    Log.Error("Create ES index failed for {indexName}. Cause: {detail}", indexName, createIndexResponse.DebugInformation);
                    throw new DatadockException(
                        $"Could not create index {indexName} for SchemaStore. Cause: {createIndexResponse.DebugInformation}");
                }
            }
            _client.ConnectionSettings.DefaultIndices[typeof(SchemaInfo)] = indexName;

        }

        public IReadOnlyCollection<SchemaInfo> GetSchemasByOwner(string ownerId, int skip, int take)
        {
            Log.Debug("GetSchemasByOwner {ownerIds}. Skip={skip}, Take={take}", ownerId, skip, take);
            var debugQuery = new SearchDescriptor<SchemaInfo>().Query(
                q => q.Bool(
                    b => b.Must(
                        bf => bf.Terms(
                            t => t.Field(f => f.OwnerId).Terms(ownerId)
                        )
                    )
                )
            ).Skip(skip).Take(take);
            using (var ms = new MemoryStream())
            {
                _client.RequestResponseSerializer.Serialize(debugQuery, ms);
                var rawQuery = Encoding.UTF8.GetString(ms.ToArray());
                Console.WriteLine(rawQuery);
            }
            var searchResponse = _client.Search<SchemaInfo>(s => s.Query(
                    q => q.Bool(
                        b => b.Must(
                            bf => bf.Terms(
                                t => t.Field(f => f.OwnerId).Terms(ownerId)
                            )
                        )
                    )
                ).Skip(skip).Take(take)
            );
            if (!searchResponse.IsValid)
            {
                Log.Error("GetSchemasByOwner Failed. OwnerId={ownerId}, Skip={skip}, Take={take}. DebugInformation: {debugInfo}",
                    ownerId, skip, take, searchResponse.DebugInformation);
                throw new SchemaStoreException(
                    $"Failed to retrieve schema list by owner. Cause: {searchResponse.DebugInformation}");
            }
            Log.Debug("GetSchemasByOwner {ownerId}. Skip={skip}, Take={take}. Returns {docCount} results", ownerId, skip, take, searchResponse.Documents.Count);
            return searchResponse.Documents;
        }

        public IReadOnlyCollection<SchemaInfo> GetSchemasByOwnerList(string[] ownerIds, int skip, int take)
        {
            Log.Debug("GetSchemasByOwnerList [{ownerIds}]. Skip={skip}, Take={take}", ownerIds, skip, take);
            var debugQuery = new SearchDescriptor<SchemaInfo>().Query(
                q => q.Bool(
                    b => b.Must(
                        bf => bf.Terms(
                            t => t.Field(f => f.OwnerId).Terms(ownerIds)
                        )
                    )
                )
            ).Skip(skip).Take(take);
            using (var ms = new MemoryStream())
            {
                _client.RequestResponseSerializer.Serialize(debugQuery, ms);
                var rawQuery = Encoding.UTF8.GetString(ms.ToArray());
                Console.WriteLine(rawQuery);
            }
            var searchResponse = _client.Search<SchemaInfo>(s => s.Query(
                    q => q.Bool(
                        b => b.Must(
                            bf => bf.Terms(
                                t => t.Field(f => f.OwnerId).Terms(ownerIds)
                            )
                        )
                    )
                ).Skip(skip).Take(take)
            );
            if (!searchResponse.IsValid)
            {
                Log.Error("GetSchemasByOwnerList Failed. OwnerIds=[{ownerIds}], Skip={skip}, Take={take}. DebugInformation: {debugInfo}",
                    ownerIds, skip, take, searchResponse.DebugInformation);
                throw new SchemaStoreException(
                    $"Failed to retrieve schema list by owner. Cause: {searchResponse.DebugInformation}");
            }
            Log.Debug("GetSchemasByOwnerList [{ownerIds}]. Skip={skip}, Take={take}. Returns {docCount} results", ownerIds, skip, take, searchResponse.Documents.Count);
            return searchResponse.Documents;
        }

        public IReadOnlyCollection<SchemaInfo> GetSchemasByRepository(string ownerId, string repositoryId, int skip, int take)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyCollection<SchemaInfo> GetSchemasByRepositoryList(string ownerId, string[] repositoryIds, int skip, int take)
        {
            Log.Debug("GetSchemasByRepositoryList [{repoIds}]. Skip={skip}, Take={take}", repositoryIds, skip, take);
            var searchResponse = _client.Search<SchemaInfo>(s => s.Query(
                    q => q.Bool(
                        b => b.Must(
                            bf => bf.Terms(
                                t => t.Field(f => f.RepositoryId).Terms(repositoryIds)
                            )
                        )
                    )
                ).Skip(skip).Take(take)
            );
            if (!searchResponse.IsValid)
            {
                Log.Error("GetSchemasByRepositoryList Failed. RepoIds=[{ownerIds}], Skip={skip}, Take={take}. DebugInformation: {debugInfo}",
                    repositoryIds, skip, take, searchResponse.DebugInformation);
                throw new SchemaStoreException(
                    $"Failed to retrieve schema list by repository. Cause: {searchResponse.DebugInformation}");
            }
            Log.Debug("GetSchemasByRepositoryList [{repoIds}]. Skip={skip}, Take={take}. Returns {docCount} results", repositoryIds, skip, take, searchResponse.Documents.Count);
            return searchResponse.Documents;
        }
        
        private static QueryContainer QueryByOwnerId(QueryContainerDescriptor<SchemaInfo> q, string ownerId)
        {
            return q.Bool(
                b => b.Must(
                    bf => bf.Match(m => m.Field(f => f.OwnerId).Query(ownerId))
                )
            );
        }

        private static QueryContainer QueryByOwnerIdAndSchemaId(QueryContainerDescriptor<SchemaInfo> q, string ownerId, string schemaId)
        {
            return q.Bool(
                b => b.Must(
                    bf => bf.Match(m => m.Field(f => f.OwnerId).Query(ownerId)),
                    bf => bf.Match(m => m.Field(f => f.SchemaId).Query(schemaId)))
            );
        }

        public async Task<SchemaInfo> GetSchemaInfoAsync(string ownerId, string schemaId)
        {
            Log.Debug("GetSchemaInfoAsync {ownerId}, {schemaId}", ownerId, schemaId);
            var searchResponse =
                await _client.SearchAsync<SchemaInfo>(s => s.Query(q=> QueryByOwnerIdAndSchemaId(q, ownerId, schemaId)));
            if (!searchResponse.IsValid)
            {
                Log.Error("GetSchemaInfoAsync Failed. OwnerId={ownerId}, SchemaId={schemaId}. DebugInformation: {debugInfo}", ownerId, schemaId, searchResponse.DebugInformation);
                throw new SchemaStoreException($"Schema search failed: {searchResponse.DebugInformation}");
            }

            if (!searchResponse.Documents.Any())
            {
                Log.Warning("GetSchemaInfoAsync {ownerId}, {schemaId} found no matches", ownerId, schemaId);
                throw new SchemaNotFoundException(ownerId, schemaId);
            }

            return searchResponse.Documents.First();
        }

        public async Task CreateOrUpdateSchemaRecordAsync(SchemaInfo schemaInfo)
        {
            var indexResponse = await _client.IndexDocumentAsync(schemaInfo);
            if (!indexResponse.IsValid)
            {
                throw new SchemaStoreException($"Failed to insert or update schema record: {indexResponse.DebugInformation}");
            }
        }

        public async Task DeleteSchemaRecordsForOwnerAsync(string ownerId)
        {
            var deleteResponse = await _client.DeleteByQueryAsync<SchemaInfo>(s => s.Query(q => QueryByOwnerId(q, ownerId)));
            if (!deleteResponse.IsValid)
            {
                throw new SchemaStoreException(
                    $"Failed to delete schema record for all schemas owned by {ownerId}");
            }
        }

        public async Task DeleteSchemaAsync(string ownerId, string schemaId)
        {
            var deleteResponse = await _client.DeleteByQueryAsync<SchemaInfo>(s => s.Query(q => QueryByOwnerIdAndSchemaId(q, ownerId, schemaId)));
            if (!deleteResponse.IsValid)
            {
                throw new SchemaStoreException(
                    $"Failed to delete schema record for schema {schemaId} owned by {ownerId}");
            }
        }
    }
}
