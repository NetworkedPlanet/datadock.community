using System;
using System.Threading;
using Datadock.Common.Elasticsearch;
using Datadock.Common.Models;
using Moq;
using Nest;
using Xunit;

namespace DataDock.Common.Tests
{
    public class JobRepositoryTests
    {

        [Fact]
        public async void SubmitImportJobRequiresNonNullRequestInfo()
        {
            var client = new Mock<IElasticClient>();
            var repo = new JobRepository(client.Object);
            await Assert.ThrowsAsync<ArgumentNullException>(() => repo.SubmitImportJobAsync(null));
        }

        [Fact]
        public async void SubmitImportJobInsertsIntoJobsIndex()
        {
            var mockResponse = new Mock<IIndexResponse>();
            mockResponse.SetupGet(x => x.IsValid).Returns(true);
            var client = new Mock<IElasticClient>();
            client.Setup(x => x.IndexDocumentAsync<JobInfo>(It.IsAny<JobInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object).Verifiable();
            var repo = new JobRepository(client.Object);
                
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
            client.Setup(x => x.IndexDocumentAsync<JobInfo>(It.IsAny<JobInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object).Verifiable();
            var repo = new JobRepository(client.Object);

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
    }
}
