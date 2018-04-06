using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Datadock.Common.Models;
using Datadock.Common.Stores;
using DataDock.Web.Api;
using DataDock.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace DataDock.Web.Tests.Api
{
    public class DataControllerTests
    {
        private readonly MemoryStream _csvStream;
        private readonly Mock<IUserStore> _mockUserStore;
        private readonly Mock<IRepoSettingsStore> _mockRepoSettingsStore;
        private readonly Mock<IFileStore> _mockFileStore;
        private readonly Mock<IJobStore> _mockJobStore;
        private readonly Mock<IImportService> _mockImportService;
        private readonly Mock<IPrincipal> _mockPrincipal;
        private readonly Mock<HttpContext> _mockHttpContext;

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


            _mockUserStore = new Mock<IUserStore>();
            _mockRepoSettingsStore = new Mock<IRepoSettingsStore>();
            _mockFileStore = new Mock<IFileStore>();
            _mockJobStore = new Mock<IJobStore>();
            _mockImportService = new Mock<IImportService>();
            _mockPrincipal = new Mock<IPrincipal>();
            _mockHttpContext = new Mock<HttpContext>();

            _mockHttpContext.Setup(m => m.Request.Method).Returns("POST");
            //_mockQueueMessage = new Mock<IConversionJobMessage>();

            //_mockJobStore.Setup(r => r.SubmitImportJobAsync(It.IsAny<ImportJobRequestInfo>(), It.IsAny<string>(), It.IsAny<string>(),
            //    It.IsAny<string>(), JobType.Import)).Returns(Task.CompletedTask).Verifiable();
            //_mockConversionQueue.Setup(q => q.QueueJobAsync(It.IsAny<ConversionJobInfo>()))
            //    .Returns(Task.FromResult(_mockQueueMessage.Object)).Verifiable();
            //_mockFileStore.SetupSequence(x => x.AddFileAsync(It.IsAny<Stream>())).ReturnsAsync("metadata_file_id").ReturnsAsync("csv_file_id");

        }

        private void WithAuthorizedUser(string userId = "test_id", string repositoryFullName = "gituser/my-repo")
        {
            _mockPrincipal.SetupGet(p => p.Identity.Name).Returns(userId);
            _mockPrincipal.SetupGet(p => p.Identity.IsAuthenticated).Returns(true);
            var claimsId = _mockPrincipal.Object as ClaimsPrincipal;
            _mockHttpContext.Setup(m => m.User).Returns(claimsId);

            var dummyRepository = new RepoSettings { RepoId = repositoryFullName };
            var dummyUserSettings = new UserSettings { };
            _mockUserStore.Setup(sr => sr.GetUserSettingsAsync(It.IsAny<string>())).Returns(Task.FromResult(dummyUserSettings));

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
            var claimsId = _mockPrincipal.Object as ClaimsPrincipal;
            _mockHttpContext.Setup(m => m.User).Returns(claimsId);
            _mockUserStore.Setup(sr => sr.GetUserSettingsAsync(It.IsAny<string>())).ReturnsAsync(new UserSettings());

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
            //Assert.IsType<OkNegotiatedContentResult<DataControllerResult>>(result);
            //var okResult = result as OkNegotiatedContentResult<DataControllerResult>;
            //Assert.NotNull(okResult);
            //Assert.True(okResult.Content.Message.Equals("API called successfully: test.csv"));
            //Assert.Equal(ValidFormMetadata, okResult.Content.Metadata);

            // Check dependency calls
            // Two files stored (metadata and csv)
            _mockFileStore.Verify(x => x.AddFileAsync(It.IsAny<Stream>()), Times.Exactly(2));
            // Job history created
            _mockJobStore.Verify(x => x.SubmitImportJobAsync(It.IsAny<ImportJobRequestInfo>()));
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

        private DataController MockDataController(MultipartFormDataContent formDataContent)
        {
            var dataController = new DataController(_mockUserStore.Object, _mockRepoSettingsStore.Object,
                _mockFileStore.Object,
                _mockJobStore.Object, _mockImportService.Object);

            //var requestMessage = new HttpRequestMessage();
            //requestMessage.RequestUri = new Uri("http://awesomeunittesting.com");
            //requestMessage.Method = HttpMethod.Post;
            //requestMessage.Content = formDataContent;

            dataController.ControllerContext = RequestWithFile(formDataContent);
            return dataController;
        }

        //Add the file in the underlying request object.
        private ControllerContext RequestWithFile(MultipartFormDataContent formDataContent)
        {
            var httpContext = _mockHttpContext.Object;
            httpContext.Request.Method = HttpMethod.Post.ToString();
            httpContext.Request.Headers.Add("Content-Type", "multipart/form-data");
            // var file = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("This is a dummy file")), 0, 0, "Data", "dummy.txt");
            // httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>(), new FormFileCollection { file });
            // TODO how to attach the form data to the request body
            
            var actx = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor());
            return new ControllerContext(actx);
        }

    }
}
