using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Datadock.Common.Elasticsearch;
using Datadock.Common.Models;
using Datadock.Common.Stores;
using FluentAssertions;
using Xunit;

namespace DataDock.IntegrationTests
{
    public class RepoSettingsStoreTests : IClassFixture<ElasticsearchFixture>
    {
        private readonly ElasticsearchFixture _fixture;
        private readonly RepoSettingsStore _store;

        public RepoSettingsStoreTests(ElasticsearchFixture fixture)
        {
            _fixture = fixture;
            _store = new RepoSettingsStore(fixture.Client, fixture.Configuration);
        }

        [Fact]
        public async Task ItCanCreateAndRetrieveRepoSettings()
        {
            var repoSettings = new RepoSettings
            {
                OwnerId = "owner-1",
                RepoId = "repo-1",
                OwnerIsOrg = false,
                LastModified = DateTime.UtcNow
            };

            await _store.CreateOrUpdateRepoSettingsAsync(repoSettings);
            Thread.Sleep(1000);
            var retrievedRepoSettings = await _store.GetRepoSettingsAsync("owner-1", "repo-1");
            retrievedRepoSettings.FullId.Should().Be($"owner-1/repo-1");
            ((string)retrievedRepoSettings.OwnerId).Should().Be("owner-1");
            ((string)retrievedRepoSettings.RepoId).Should().Be("repo-1");
            (retrievedRepoSettings.OwnerIsOrg).Should().BeFalse();
            retrievedRepoSettings.LastModified.Should().BeCloseTo(repoSettings.LastModified);
        }

    }

    public class RepoSettingsStoreFixture : ElasticsearchFixture
    {
        public RepoSettingsStore Store { get; }
        public RepoSettingsStoreFixture() : base()
        {
            Store = new RepoSettingsStore(Client, Configuration);
            InitializeRepository().Wait();
            Thread.Sleep(1000);
        }

        private async Task InitializeRepository()
        {
            for (var o = 0; o < 5; o++)
            {
                for (var r = 0; r < 5; r++)
                {
                    var repoSettings = new RepoSettings
                    {
                        OwnerId = "owner_" + o,
                        RepoId = "repo_" + r,
                        LastModified = DateTime.UtcNow
                    };

                    await Store.CreateOrUpdateRepoSettingsAsync(repoSettings);
                }
            }

        }

    }

    public class RepoSettingsStoreSearchTests : IClassFixture<RepoSettingsStoreFixture>
    {
        private readonly RepoSettingsStore _store;

        public RepoSettingsStoreSearchTests(RepoSettingsStoreFixture fixture)
        {
            _store= fixture.Store;
        }


        [Fact]
        public async void ItCanRetrieveMultipleRepoSettingsForASingleOwner()
        {
            var results = await _store.GetRepoSettingsAsync("owner_0", "repo_0");
            results.Should().NotBeNull();
            var rs = results as RepoSettings;
            rs.Should().NotBeNull();
            rs.OwnerId.Should().Be("owner_0");
            rs.RepoId.Should().Be("repo_0");
        }
        [Fact]
        public void ItShouldReturnNoResultsByOwnerWhenNoneExist()
        {
            var ex = Assert.ThrowsAsync<RepoSettingsNotFoundException>(async () =>
                await _store.GetRepoSettingsForOwnerAsync("owner_100"));

            Assert.Equal($"No repo settings found with ownerId owner_100", ex.Result.Message);
        }


        [Fact]
        public async void ItCanRetrieveRepoSettingsForSingleRepository()
        {
            var results = await _store.GetRepoSettingsAsync("owner_0", "repo_0");
            results.Should().NotBeNull();
            var rs = results as RepoSettings;
            rs.Should().NotBeNull();
            rs.OwnerId.Should().Be("owner_0");
            rs.RepoId.Should().Be("repo_0");
        }

        [Fact]
        public void ItShouldReturnNoResultsByRepositoryIdWhenNoneExist()
        {
            var ex = Assert.ThrowsAsync<RepoSettingsNotFoundException>(async () =>
                await _store.GetRepoSettingsAsync("owner_0", "repo_100"));

            Assert.Equal($"No repo settings found with ownerId owner_0 and repositoryId repo_100", ex.Result.Message);
        }
    }
}
