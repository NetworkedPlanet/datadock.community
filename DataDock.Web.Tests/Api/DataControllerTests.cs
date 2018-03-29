using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace DataDock.Web.Tests.Api
{
    public class DataControllerTests
    {
        private readonly MemoryStream _csvStream;
        private readonly Mock<ISettingsRepository> _mockSettingsRepo;
        private readonly Mock<IJobStore> _mockJobFileStore;
        private readonly Mock<IJobHistoryRepository> _mockJobHistoryRepo;
        private readonly Mock<IConversionJobQueue> _mockConversionQueue;
        private readonly Mock<IPrincipal> _mockPrincipal;
        private readonly Mock<HttpRequestContext> _mockRequestContext;
        private readonly Mock<IConversionJobMessage> _mockQueueMessage;

        private const string ValidFormMetadata = @"{ 'url': 'http://datadock.io/gituser/my-repo/test.csv' }";

        private const string ValidRepoJson = @"{
'repositoryId': 'gituser/my-repo',
'cloneUrl':'https://github.com/gituser/my-repo.git',
'ownerLogin':'gituser',
'name':'my-repo'
}";
        private readonly Dictionary<string, string> _validForm = new Dictionary<string, string>
        {
            {"metadata", ValidFormMetadata },
            {"targetRepository", ValidRepoJson },
            {"showOnHomePage", "true" },
            {"overwriteExisting", "true" }
        };

        public DataControllerTests()
        {
            _csvStream = new MemoryStream();
            using (var writer = new StreamWriter(_csvStream))
            {
                writer.WriteLine("Col1,Col2,Col3");
                writer.WriteLine("A,B,C");
            }
            _csvStream = new MemoryStream(_csvStream.GetBuffer());


            _mockSettingsRepo = new Mock<ISettingsRepository>();
            _mockJobFileStore = new Mock<IJobFileStore>();
            _mockJobHistoryRepo = new Mock<IJobHistoryRepository>();
            _mockConversionQueue = new Mock<IConversionJobQueue>();
            _mockPrincipal = new Mock<IPrincipal>();
            _mockRequestContext = new Mock<HttpRequestContext>();
            _mockQueueMessage = new Mock<IConversionJobMessage>();

            _mockJobHistoryRepo.Setup(r => r.CreateAsync("test_id", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), JobType.Import)).Returns(Task.CompletedTask).Verifiable();
            _mockConversionQueue.Setup(q => q.QueueJobAsync(It.IsAny<ConversionJobInfo>()))
                .Returns(Task.FromResult(_mockQueueMessage.Object)).Verifiable();
            _mockJobFileStore.SetupSequence(x => x.AddFileAsync(It.IsAny<Stream>())).ReturnsAsync("metadata_file_id").ReturnsAsync("csv_file_id");

        }

        private void WithAuthorizedUser(string userId = "test_id", string repositoryFullName = "gituser/my-repo")
        {
            _mockPrincipal.SetupGet(p => p.Identity.Name).Returns(userId);
            _mockPrincipal.SetupGet(p => p.Identity.IsAuthenticated).Returns(true);
            _mockRequestContext.SetupGet(c => c.Principal).Returns(_mockPrincipal.Object);

            var dummyRepository = new RepositorySettings { RepositoryId = repositoryFullName };
            var dummyUserSettings = new UserSettings { };
            _mockSettingsRepo.Setup(sr => sr.GetUserSettingsAsync(It.IsAny<string>())).Returns(Task.FromResult(dummyUserSettings));

        }

        private DataController MockDataController(HttpContent requestContent)
        {
            return new DataController(_mockSettingsRepo.Object, _mockJobFileStore.Object,
                _mockJobHistoryRepo.Object, _mockConversionQueue.Object)
            {
                RequestContext = _mockRequestContext.Object,
                Request = new HttpRequestMessage
                {
                    Content = requestContent
                }
            };
        }

        [Fact]
        public void AnonymousUserNotAllowed()
        {
            // No user added to request context - simulating anonymous access
            var metadataJson = @"{'foo':'bar'}";
            var controller = MockDataController(CreateFormData(new Dictionary<string, string> { { "metadata", metadataJson } }));
            var result = controller.Post().Result;
            Assert.NotNull(result);
            var unauthorizedResult = result as UnauthorizedResult;
            Assert.NotNull(unauthorizedResult);
        }

        [Fact]
        public void UnauthenticatedUserNotAllowed()
        {
            // User added to request context but IsAuthenticated is false - simulating unauthenticated access
            _mockPrincipal.SetupGet(p => p.Identity.Name).Returns("test_id");
            _mockPrincipal.SetupGet(p => p.Identity.IsAuthenticated).Returns(false);
            _mockRequestContext.SetupGet(c => c.Principal).Returns(_mockPrincipal.Object);
            _mockSettingsRepo.Setup(sr => sr.GetUserSettingsAsync(It.IsAny<string>())).ReturnsAsync(new UserSettings());

            var metadataJson = @"{'foo':'bar'}";
            var controller = MockDataController(CreateFormData(new Dictionary<string, string> { { "metadata", metadataJson } }));
            var result = controller.Post().Result;
            Assert.NotNull(result);
            var unauthorizedResult = result as UnauthorizedResult;
            Assert.NotNull(unauthorizedResult);
        }

        [Fact]
        public void SimpleValidRequest()
        {
            WithAuthorizedUser("test_id", "gituser/my-repo");
            var controller = MockDataController(CreateFormData(_validForm));
            var result = controller.Post().Result;
            Assert.NotNull(result);
            Assert.IsType<OkNegotiatedContentResult<DataControllerResult>>(result);
            var okResult = result as OkNegotiatedContentResult<DataControllerResult>;
            Assert.NotNull(okResult);
            Assert.True(okResult.Content.Message.Equals("API called successfully: test.csv"));
            Assert.Equal(ValidFormMetadata, okResult.Content.Metadata);

            // Check dependency calls
            // Two files stored (metadata and csv)
            _mockJobFileStore.Verify(x => x.AddFileAsync(It.IsAny<Stream>()), Times.Exactly(2));
            // Job history created
            _mockJobHistoryRepo.Verify(x => x.CreateAsync(
                "test_id",
                It.Is<string>(jobId => jobId.StartsWith("test_id_")),
                "gituser",
                "gituser/my-repo",
                "http://datadock.io/gituser/my-repo/test.csv",
                JobType.Import),
                Times.Once);
            // Job queued
            _mockConversionQueue.Verify(q => q.QueueJobAsync(It.Is<ConversionJobInfo>(
                j => j.JobId.StartsWith("test_id_") &&
                j.UserId.Equals("test_id") &&
                j.Repository.Equals("https://github.com/gituser/my-repo.git") &&
                j.GitRepositoryOwnerId.Equals("gituser") &&
                j.GitRepositoryName.Equals("my-repo") &&
                j.GitRepositoryFullName.Equals("gituser/my-repo") &&
                j.DatasetId.Equals("test.csv") &&
                j.CsvFileName.Equals("test.csv") &&
                j.DatasetIri.Equals("http://datadock.io/gituser/my-repo/test.csv") &&
                j.CsvFileId.Equals("csv_file_id") &&
                j.CsvmFileId.Equals("metadata_file_id") &&
                j.JobType.Equals("Import") &&
                j.ShowOnHomePage.Equals(true) &&
                j.OverwriteExistingData.Equals(true)
                )));
        }

        [Fact]
        public void UserWithNoSettingsNotAllowed()
        {
            // User added to request context - but no settings from user settings repo
            _mockPrincipal.SetupGet(p => p.Identity.Name).Returns("test_id");
            _mockPrincipal.SetupGet(p => p.Identity.IsAuthenticated).Returns(true);
            _mockRequestContext.SetupGet(c => c.Principal).Returns(_mockPrincipal.Object);
            _mockSettingsRepo.Setup(sr => sr.GetUserSettingsAsync(It.IsAny<string>())).ReturnsAsync((UserSettings)null);

            var metadataJson = @"{'foo':'bar'}";
            var controller = MockDataController(CreateFormData(new Dictionary<string, string> { { "metadata", metadataJson } }));
            var result = controller.Post().Result;
            Assert.NotNull(result);
            var unauthorizedResult = result as UnauthorizedResult;
            Assert.NotNull(unauthorizedResult);
        }

        [Fact]
        public void ContentTypeMustBeFormData()
        {
            WithAuthorizedUser("test_id", "gituser/my-repo");
            var controller = MockDataController(new StringContent("Hello world", Encoding.UTF8, "text/plain"));
            var result = controller.Post().Result;
            Assert.IsType<BadRequestErrorMessageResult>(result);
            var badRequest = result as BadRequestErrorMessageResult;
            Assert.Equal("Unsupported media type", badRequest.Message);
        }

        [Fact]
        public void MetadataFormFieldRequired()
        {
            WithAuthorizedUser();
            var formValues = new Dictionary<string, string>(_validForm);
            formValues.Remove("metadata");
            var controller = MockDataController(CreateFormData(formValues));
            var result = controller.Post().Result;
            Assert.IsType<BadRequestErrorMessageResult>(result);
            var badRequest = result as BadRequestErrorMessageResult;
            Assert.Equal("No metadata supplied", badRequest.Message);
        }

        [Fact]
        public void TargetRepositoryFormFieldRequired()
        {
            WithAuthorizedUser();
            var formValues = new Dictionary<string, string>(_validForm);
            formValues.Remove("targetRepository");
            var controller = MockDataController(CreateFormData(formValues));
            var result = controller.Post().Result;
            Assert.IsType<BadRequestErrorMessageResult>(result);
            var badRequest = result as BadRequestErrorMessageResult;
            Assert.Equal("No target repository supplied", badRequest.Message);
        }

        [Fact]
        public void ParseBooleanFalseValues()
        {
            WithAuthorizedUser();
            var formValues = new Dictionary<string, string>(_validForm);
            formValues["showOnHomePage"] = "false";
            formValues["overwriteExisting"] = "False";
            var controller = MockDataController(CreateFormData(formValues));
            var result = controller.Post().Result;
            Assert.IsType<OkNegotiatedContentResult<DataControllerResult>>(result);
            var okResult = result as OkNegotiatedContentResult<DataControllerResult>;
            okResult.Content.Message.Should().Be("API called successfully: test.csv");
            _mockConversionQueue.Verify(
                q => q.QueueJobAsync(
                    It.Is<ConversionJobInfo>(j =>
                        j.ShowOnHomePage == false &&
                        j.OverwriteExistingData == false)));
        }

        [Fact]
        public void AFileIsRequired()
        {
            WithAuthorizedUser();
            var contentWithNoFile = new MultipartFormDataContent();
            foreach (var entry in _validForm)
            {
                contentWithNoFile.Add(new StringContent(entry.Value), entry.Key);
            }
            var controller = MockDataController(contentWithNoFile);
            var result = controller.Post().Result;
            Assert.IsType<BadRequestErrorMessageResult>(result);
            var badRequest = (BadRequestErrorMessageResult)result;
            Assert.Equal("No files supplied", badRequest.Message);
        }

        private MultipartFormDataContent CreateFormData(Dictionary<string, string> formValues, Stream fileStream = null,
            string fileName = "test.csv")
        {
            if (fileStream == null) fileStream = _csvStream;

            var formData = new MultipartFormDataContent();
            foreach (var entry in formValues)
            {
                formData.Add(new StringContent(entry.Value), entry.Key);
            }
            formData.Add(new StreamContent(fileStream), "csv", fileName);
            return formData;
        }
    }
}
