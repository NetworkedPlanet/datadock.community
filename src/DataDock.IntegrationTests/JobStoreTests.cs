using System;
using System.Collections.Generic;
using System.Text;
using Datadock.Common.Elasticsearch;
using Datadock.Common.Models;
using DataDock.Common;
using Nest;
using Xunit;

namespace DataDock.IntegrationTests
{
    public class JobStoreTests : IClassFixture<ElasticsearchFixture>, IDisposable
    {
        private readonly JobStore _repo;
        private readonly ElasticsearchFixture _fixture;

        public JobStoreTests(ElasticsearchFixture esFixture)
        {
            _fixture = esFixture;
            var config =
                new ApplicationConfiguration(null, esFixture.JobsIndexName, null, null, null, null, null, null);
            _repo = new JobStore(esFixture.Client, config);
        }

        public void Dispose()
        {

        }

        [Fact]
        public async void CanCreateAndRetrieveAnImportJob()
        {
            await _fixture.Client.DeleteByQueryAsync<JobInfo>(s=>s.MatchAll());
            var jobInfo = await _repo.SubmitImportJobAsync(new ImportJobRequestInfo
            {
                UserId = "user",
                OwnerId = "owner",
                RepositoryId = "repo",
                DatasetId = "dataset",
                DatasetIri = "http://datadock.io/owner/repo/dataset",
                CsvFileName = "data.csv",
                CsvFileId = "fileid",
                CsvmFileId = "fileid",
                IsPublic = true,
                OverwriteExistingData = false
            });
            Assert.NotNull(jobInfo.JobId);
            Assert.NotEmpty(jobInfo.JobId);
            Assert.True(jobInfo.QueuedTimestamp > 0);
            Assert.True(jobInfo.QueuedTimestamp <= DateTime.UtcNow.Ticks);

            var retrievedJobInfo = await _repo.GetJobInfoAsync(jobInfo.JobId);
            Assert.Equal("user", retrievedJobInfo.UserId);
            Assert.Equal("owner", retrievedJobInfo.OwnerId);
            Assert.Equal("repo", retrievedJobInfo.RepositoryId);
        }
    }
}
