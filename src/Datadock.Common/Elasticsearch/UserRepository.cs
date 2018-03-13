﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Datadock.Common.Models;
using Datadock.Common.Repositories;
using Datadock.Common.Validators;
using Nest;
using Serilog;

namespace Datadock.Common.Elasticsearch
{
    public class UserRepository : IUserRepository
    {
        private readonly ElasticClient _client;

        public UserRepository(ElasticClient client, string userSettingsIndexName, string userAccountIndexName)
        {
            _client = client;
            // Ensure the index exists
            var indexExistsReponse = _client.IndexExists(userSettingsIndexName);
            if (!indexExistsReponse.Exists)
            {
                Log.Debug("Create ES index {indexName} for type {indexType}", userSettingsIndexName, typeof(UserSettings));
                var createIndexResponse = _client.CreateIndex(userSettingsIndexName, config =>
                    config.Mappings(mappings => mappings.Map<UserSettings>(m => m.AutoMap(-1))));
                if (!createIndexResponse.Acknowledged)
                {
                    Log.Error("Create ES index failed for {indexName}. Cause: {detail}", userSettingsIndexName, createIndexResponse.DebugInformation);
                    throw new DatadockException(
                        $"Could not create index {userSettingsIndexName} for UserRepository. Cause: {createIndexResponse.DebugInformation}");
                }
            }

            // Repeat for user account index
            indexExistsReponse = _client.IndexExists(userAccountIndexName);
            if (!indexExistsReponse.Exists)
            {
                Log.Debug("Create ES index {indexName} for type {indexType}", userAccountIndexName, typeof(UserAccount));
                var createIndexResponse = _client.CreateIndex(userAccountIndexName, config =>
                    config.Mappings(mappings => mappings.Map<UserAccount>(m => m.AutoMap(-1))));
                if (!createIndexResponse.Acknowledged)
                {
                    Log.Error("Create ES index failed for {indexName}. Cause: {detail}", userAccountIndexName, createIndexResponse.DebugInformation);
                    throw new DatadockException(
                        $"Could not create index {userAccountIndexName} for UserRepository. Cause: {createIndexResponse.DebugInformation}");
                }
            }

        }

        public async Task<UserSettings> GetUserSettingsAsync(string userId)
        {
            var response = await _client.GetAsync<UserSettings>(userId);
            if (!response.IsValid)
            {
                if (!response.Found) throw new UserAccountNotFoundException(userId);
                throw new UserRepositoryException(
                    $"Error retrieving user account for user ID {userId}. Cause: {response.DebugInformation}");
            }
            return response.Source;
        }

        public async Task CreateOrUpdateUserSettingsAsync(UserSettings userSettings)
        {
            if (userSettings == null) throw new ArgumentNullException(nameof(userSettings));
            var validator = new UserSettingsValidator();
            var validationResults = await validator.ValidateAsync(userSettings);
            if (!validationResults.IsValid)
            {
                throw new ValidationException("Invalid user settings", validationResults);
            }
            var updateResponse = await _client.IndexDocumentAsync(userSettings);
            if (!updateResponse.IsValid)
            {
                throw new UserRepositoryException($"Error udpating user settings for user ID {userSettings.UserId}");
            }
        }

        public async Task<UserAccount> CreateUserAsync(string userId, IEnumerable<Claim> claims)
        {
            var user = new UserAccount
            {
                UserId = userId,
                Claims = claims.Select(c=>new AccountClaim(c)).ToList()
            };
             await _client.IndexDocumentAsync(user);
            return user;
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            var response = await _client.DeleteAsync<UserAccount>(userId);
            return response.IsValid;
        }

        public async Task<UserAccount> GetUserAccountAsync(string userId)
        {
            var response = await _client.GetAsync<UserAccount>(userId);
            if (!response.Found) throw new UserAccountNotFoundException(userId);
            return response.Source;
        }

        public async Task<bool> ValidateLastChanged(string userId, DateTime lastChanged)
        {
            throw new NotImplementedException();
        }
    }
}
