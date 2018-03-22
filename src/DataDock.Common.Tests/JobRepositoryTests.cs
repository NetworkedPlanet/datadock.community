using System;
using System.Threading;
using Datadock.Common.Elasticsearch;
using Datadock.Common.Models;
using Datadock.Common.Repositories;
using Moq;
using Nest;
using Xunit;

namespace DataDock.Common.Tests
{
    public class JobRepositoryTests
    {
        [Fact]
        public void RepositoryCreatesIndexIfItDoesNotExist()
        {
            var client = new Mock<IElasticClient>();
            var notExists = new Mock<IExistsResponse>();
            notExists.SetupGet(x => x.Exists).Returns(false);
            var indexCreated = new Mock<ICreateIndexResponse>();
            indexCreated.SetupGet(x => x.Acknowledged).Returns(true);
            client.Setup(x => x.IndexExists(It.IsAny<Indices>(), null)).Returns(notExists.Object);
            client.Setup(x => x.CreateIndex("jobs", It.IsAny<Func<CreateIndexDescriptor, ICreateIndexRequest>>()))
                .Returns(indexCreated.Object).Verifiable();
            var repo = new JobRepository(client.Object, new ApplicationConfiguration(null, "jobs", null, null, null, null, null, null));
            client.Verify();
        }

        [Fact]
        public async void SubmitImportJobRequiresNonNullRequestInfo()
        {
            var client = new Mock<IElasticClient>();
            AssertIndexExists(client, "jobs");
            var repo = new JobRepository(client.Object, new ApplicationConfiguration(null, "jobs", null, null, null, null, null, null));
            await Assert.ThrowsAsync<ArgumentNullException>(() => repo.SubmitImportJobAsync(null));
        }

        [Fact]
        public async void SubmitImportJobInsertsIntoJobsIndex()
        {
            var mockResponse = new Mock<IIndexResponse>();
            mockResponse.SetupGet(x => x.IsValid).Returns(true);
            var client = new Mock<IElasticClient>();
            AssertIndexExists(client, "jobs");
            client.Setup(x => x.IndexDocumentAsync<JobInfo>(It.IsAny<JobInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object).Verifiable();
            var repo = new JobRepository(client.Object, new ApplicationConfiguration(null, "jobs", null, null, null, null, null, null));
                
            var jobRequest = new ImportJobRequestInfo
            {
                UserId = "user",
                OwnerId = "owner",
                RepositoryId = "repo",
                DatasetId = "dataset",
                DatasetIri = "https://datadock.io/owner/repo/dataset",
                CsvFileName = "dataset.csv",
                CsvFileId = "csvfileid",
                CsvmFileId = "csvmfileid",
                IsPublic = true,
                OverwriteExistingData = false
            };

            var jobInfo = await repo.SubmitImportJobAsync(jobRequest);

            client.Verify();
        }

        [Fact]
        public async void SubmitJobThrowsWhenInsertFails()
        {
            var mockResponse = new Mock<IIndexResponse>();
            mockResponse.SetupGet(x => x.IsValid).Returns(false);
            var client = new Mock<IElasticClient>();
            AssertIndexExists(client, "jobs");
            client.Setup(x => x.IndexDocumentAsync<JobInfo>(It.IsAny<JobInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object).Verifiable();
            var repo = new JobRepository(client.Object, new ApplicationConfiguration(null, "jobs", null, null, null, null, null, null));

            var jobRequest = new ImportJobRequestInfo
            {
                UserId = "user",
                OwnerId = "owner",
                RepositoryId = "repo",
                DatasetId = "dataset",
                DatasetIri = "https://datadock.io/owner/repo/dataset",
                CsvFileName = "dataset.csv",
                CsvFileId = "csvfileid",
                CsvmFileId = "csvmfileid",
                IsPublic = true,
                OverwriteExistingData = false
            };

            await Assert.ThrowsAsync<JobRepositoryException>(() => repo.SubmitImportJobAsync(jobRequest));

            client.Verify();
        }

        private static void AssertIndexExists(Mock<IElasticClient> client, string indexName)
        {
            var indexExistsResult = new Mock<IExistsResponse>();
            indexExistsResult.SetupGet(x => x.Exists).Returns(true);

            client.Setup(x => x.IndexExists(It.IsAny<Indices>(), null))
                .Returns(indexExistsResult.Object);
        }
    }
}
