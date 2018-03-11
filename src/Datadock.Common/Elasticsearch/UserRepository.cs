using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Datadock.Common.Models;
using Datadock.Common.Repositories;
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
            return response.Source;
        }

        public async Task CreateOrUpdateUserSettingsAsync(UserSettings userSettings)
        {
            await _client.IndexDocumentAsync(userSettings);
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
    }

    public class UserAccountNotFoundException : UserRepositoryException
    {
        public UserAccountNotFoundException(string userId) : base($"Could not find account record for user {userId}")
        {
        }
    }

    public class UserRepositoryException : DatadockException
    {
        public UserRepositoryException(string msg) : base(msg) { }
    }
}
