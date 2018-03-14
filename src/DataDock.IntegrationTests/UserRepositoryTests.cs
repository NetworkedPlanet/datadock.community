﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Datadock.Common.Elasticsearch;
using Datadock.Common.Models;
using Xunit;

namespace DataDock.IntegrationTests
{
    public class UserRepositoryTests : IClassFixture<ElasticsearchFixture>
    {
        private readonly ElasticsearchFixture _esFixture;
        private UserRepository _userRepository;

        public UserRepositoryTests(ElasticsearchFixture fixture)
        {
            _esFixture = fixture;
            _userRepository = new UserRepository(_esFixture.Client, _esFixture.UserSettingsIndexName, _esFixture.UserAccountsIndexName);
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
    }
}
