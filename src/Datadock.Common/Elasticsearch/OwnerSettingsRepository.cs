﻿using Datadock.Common.Models;
using Datadock.Common.Repositories;
using Nest;
using Serilog;
using System;
using System.Threading.Tasks;
using Datadock.Common.Validators;

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
                    config.Mappings(mappings => mappings.Map<OwnerSettings>(m => m.AutoMap(-1))));
                if (!createIndexResponse.Acknowledged)
                {
                    Log.Error("Create ES index failed for {indexName}. Cause: {detail}", indexName, createIndexResponse.DebugInformation);
                    throw new DatadockException(
                        $"Could not create index {indexName} for owner settings repository. Cause: {createIndexResponse.DebugInformation}");
                }
            }

            _client.ConnectionSettings.DefaultIndices[typeof(OwnerSettings)] = indexName;
        }

        public async Task<OwnerSettings> GetOwnerSettingsAsync(string ownerId)
        {
            var response = await _client.GetAsync<OwnerSettings>(ownerId);
            if (!response.IsValid)
            {
                if (!response.Found) throw new OwnerSettingsNotFoundException(ownerId);
                throw new OwnerSettingsRepositoryException(
                    $"Error retrieving owner settings for owner ID {ownerId}. Cause: {response.DebugInformation}");
            }
            return response.Source;
        }

        public async Task CreateOrUpdateOwnerSettingsAsync(OwnerSettings ownerSettings)
        {
            if (ownerSettings == null) throw new ArgumentNullException(nameof(ownerSettings));
            var validator = new OwnerSettingsValidator();
            var validationResults = await validator.ValidateAsync(ownerSettings);
            if (!validationResults.IsValid)
            {
                throw new ValidationException("Invalid owner settings", validationResults);
            }
            var updateResponse = await _client.IndexDocumentAsync(ownerSettings);
            if (!updateResponse.IsValid)
            {
                throw new OwnerSettingsRepositoryException($"Error udpating owner settings for owner ID {ownerSettings.OwnerId}");
            }
        }
    }
}

