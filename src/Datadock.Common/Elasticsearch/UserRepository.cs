using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Datadock.Common.Models;
using Datadock.Common.Repositories;
using Datadock.Common.Validators;
using Nest;

namespace Datadock.Common.Elasticsearch
{
    public class UserRepository : IUserRepository
    {
        private readonly ElasticClient _client;

        public UserRepository(ElasticClient client)
        {
            _client = client;
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
