using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Datadock.Common.Elasticsearch;
using Datadock.Common.Models;
using DataDock.Common;
using Xunit;

namespace DataDock.IntegrationTests
{
    public class UserRepositoryTests : IClassFixture<ElasticsearchFixture>
    {
        private readonly UserRepository _userRepository;

        public UserRepositoryTests(ElasticsearchFixture fixture)
        {
            var esFixture = fixture;
            var config = new ApplicationConfiguration(null, null, esFixture.UserAccountsIndexName, null, null, null,
                null, null);
            _userRepository = new UserRepository(esFixture.Client, config);
        }

        [Fact]
        public async void CreateAndRetrieveUserAccount()
        {
            var accountClaims = new List<Claim>()
            {
                new Claim(ClaimTypes.Email, "test1@example.org"),
                new Claim(ClaimTypes.Name, "Test User Name"),
                new Claim(DataDockClaimTypes.GitHubAccessToken, "some_access_token_value")
            };
            var userAccount = await _userRepository.CreateUserAsync("create1", accountClaims);
            Assert.NotNull(userAccount);
            Assert.Equal("create1", userAccount.UserId);
            foreach (var claim in accountClaims)
            {
                Assert.Contains(userAccount.AccountClaims, c =>c.Type.Equals(claim.Type) && c.Value.Equals(claim.Value));
                Assert.Contains(userAccount.Claims, c => c.Type.Equals(claim.Type) && c.Value.Equals(claim.Value));
            }

            var retrievedAccount = await _userRepository.GetUserAccountAsync("create1");
            Assert.NotNull(retrievedAccount);
            Assert.Equal("create1", retrievedAccount.UserId);
            Assert.Equal(3, retrievedAccount.Claims.Count());
            foreach (var claim in accountClaims)
            {
                Assert.Contains(userAccount.AccountClaims, c => c.Type.Equals(claim.Type) && c.Value.Equals(claim.Value));
                Assert.Contains(userAccount.Claims, c => c.Type.Equals(claim.Type) && c.Value.Equals(claim.Value));
            }
        }

        [Fact]
        public async void UpdateExistingUserAccount()
        {
            var initialAccountClaims = new List<Claim>()
            {
                new Claim(ClaimTypes.Email, "test1@example.org"),
                new Claim(ClaimTypes.Name, "Test User Name"),
                new Claim(DataDockClaimTypes.GitHubAccessToken, "some_access_token_value")
            };
            var updatedAccountClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, "updated@example.org"),
                new Claim(ClaimTypes.Name, "Updated User Name"),
                new Claim(DataDockClaimTypes.GitHubAccessToken, "some_access_token_value")
            };
            var userAccount = await _userRepository.CreateUserAsync("update1", initialAccountClaims);
            var updatedAccount = await _userRepository.UpdateUserAsync("update1", updatedAccountClaims);
            var retrievedAccount = await _userRepository.GetUserAccountAsync("update1");
            Assert.Equal("update1", retrievedAccount.UserId);
            Assert.Equal(3, retrievedAccount.Claims.Count());
            foreach (var claim in updatedAccountClaims)
            {
                Assert.Contains(retrievedAccount.AccountClaims, c => c.Type.Equals(claim.Type) && c.Value.Equals(claim.Value));
                Assert.Contains(retrievedAccount.Claims, c => c.Type.Equals(claim.Type) && c.Value.Equals(claim.Value));
            }
        }

    }
}
