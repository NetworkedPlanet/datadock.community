using System;
using System.Collections.Generic;
using System.Threading;
using Datadock.Common.Elasticsearch;
using Datadock.Common.Models;
using Elasticsearch.Net;
using Moq;
using Nest;
using Xunit;

namespace DataDock.Common.Tests
{
    public class JobRepositoryTests
    {

        [Fact]
        public async void SubmitJobRequiresNonNullRequestInfo()
        {
            var client = new Mock<IElasticClient>();
            var repo = new JobRepository(client.Object);
            await Assert.ThrowsAsync<ArgumentNullException>(() => repo.SubmitJobAsync(null));
        }

        [Fact]
        public async void SubmitJobInsertsIntoJobsIndex()
        {
            var mockResponse = new Mock<IIndexResponse>();
            mockResponse.SetupGet(x => x.IsValid).Returns(true);
            var client = new Mock<IElasticClient>();
            client.Setup(x => x.IndexDocumentAsync<JobInfo>(It.IsAny<JobInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object).Verifiable();
            var repo = new JobRepository(client.Object);
                
            var jobRequest = new JobRequestInfo
            {
                JobType = JobType.Import,
                UserId = "user",
                OwnerId = "owner",
                RepositoryId = "repo",
                Parameters = new Dictionary<string, string>
                {
                    {"param1", "value1"},
                    {"param2", "value2"}
                }
            };

            var jobId = await repo.SubmitJobAsync(jobRequest);

            client.Verify();
        }

        [Fact]
        public async void SubmitJobThrowsWhenInsertFails()
        {
            var mockResponse = new Mock<IIndexResponse>();
            mockResponse.SetupGet(x => x.IsValid).Returns(false);
            var client = new Mock<IElasticClient>();
            client.Setup(x => x.IndexDocumentAsync<JobInfo>(It.IsAny<JobInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object).Verifiable();
            var repo = new JobRepository(client.Object);

            var jobRequest = new JobRequestInfo
            {
                JobType = JobType.Import,
                UserId = "user",
                OwnerId = "owner",
                RepositoryId = "repo",
                Parameters = new Dictionary<string, string>
                {
                    {"param1", "value1"},
                    {"param2", "value2"}
                }
            };

            await Assert.ThrowsAsync<JobRepositoryException>(() => repo.SubmitJobAsync(jobRequest));

            client.Verify();

        }
    }
}
