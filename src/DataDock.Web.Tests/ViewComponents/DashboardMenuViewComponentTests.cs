using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Datadock.Common.Models;
using Datadock.Common.Stores;
using DataDock.Web.ViewComponents;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Moq;
using Xunit;

namespace DataDock.Web.Tests.ViewComponents
{
    public class DashboardMenuViewComponentTests
    {
        private readonly Mock<HttpContext> _mockHttpContext;
        private readonly Mock<IRepoSettingsStore> _mockRepoSettingsStore;

        public DashboardMenuViewComponentTests()
        {
            _mockHttpContext = new Mock<HttpContext>();
            _mockRepoSettingsStore = new Mock<IRepoSettingsStore>();

            var repo1 = new RepoSettings
            {
                OwnerId = "owner-1",
                RepoId = "repo-1",
                FullId = "owner-1/repo-1",
                OwnerIsOrg = false
            };
            var repo2 = new RepoSettings
            {
                OwnerId = "owner-1",
                RepoId = "repo-2",
                FullId = "owner-1/repo-2",
                OwnerIsOrg = false
            };
            var repo3 = new RepoSettings
            {
                OwnerId = "owner-1",
                RepoId = "repo-3",
                FullId = "owner-1/repo-3",
                OwnerIsOrg = false
            };
            var repos = new List<RepoSettings> { repo1, repo2, repo3 };

            _mockRepoSettingsStore.Setup(m => m.GetRepoSettingsForOwnerAsync(It.IsAny<string>()))
                .Returns(Task.FromResult<IEnumerable<RepoSettings>>(repos));
        }

        private ViewComponentContext GetViewContext(string withUserId)
        {
            if (!string.IsNullOrEmpty(withUserId))
            {
                WithAuthorizedUser(withUserId);
            }
            var viewContext = new ViewContext {HttpContext = _mockHttpContext.Object};
            var viewComponentContext = new ViewComponentContext {ViewContext = viewContext};
            return viewComponentContext;
        }

        private void WithAuthorizedUser(string userId = "test_id")
        {
            var name = new Claim(ClaimTypes.Name, userId);
            var ghUser = new Claim(DataDockClaimTypes.GitHubUser, userId);
            var ghLogin = new Claim(DataDockClaimTypes.GitHubLogin, userId);
            var ghName = new Claim(DataDockClaimTypes.GitHubName, userId);

            var testPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {name, ghUser, ghLogin, ghName}, "MockUserAuthentication"));

            _mockHttpContext.Setup(m => m.User).Returns(testPrincipal);
        }
        
        [Fact]
        public void ViewComponentLoadsPublicWithOwnerId()
        {
            var ownerId = "owner-1";
            var area = "summary";
            var vc = new DashboardMenuViewComponent(_mockRepoSettingsStore.Object);
            var asyncResult = vc.InvokeAsync(ownerId, "", area);
            
            Assert.NotNull(asyncResult.Result);
            _mockRepoSettingsStore.Verify(m => m.GetRepoSettingsForOwnerAsync(ownerId), Times.Never);
            var result = asyncResult.Result as ViewViewComponentResult;
            Assert.NotNull(result);
            var model = result.ViewData?.Model as DashboardMenuViewModel;
            Assert.NotNull(model);

            Assert.Equal(ownerId, model.SelectedOwnerId);
            Assert.Equal(area, model.ActiveArea);
            Assert.Equal("", model.SelectedRepoId);
            Assert.Null(model.UserViewModel);
        }

        [Fact]
        public void ViewComponentLoadsPrivateWithOwnerId()
        {
            var userName = "owner-1";
            var ownerId = "owner-1";
            var area = "summary";

            var vc = new DashboardMenuViewComponent(_mockRepoSettingsStore.Object);
            // Set user
            vc.ViewComponentContext = GetViewContext(userName);

            var asyncResult = vc.InvokeAsync(ownerId, "", area);

            Assert.NotNull(asyncResult.Result);
            
            var result = asyncResult.Result as ViewViewComponentResult;
            Assert.NotNull(result);
            var model = result.ViewData?.Model as DashboardMenuViewModel;
            Assert.NotNull(model);

            Assert.Equal(ownerId, model.SelectedOwnerId);
            Assert.Equal(area, model.ActiveArea);
            Assert.Equal("", model.SelectedRepoId);
            Assert.NotNull(model.UserViewModel);
            Assert.Equal(userName, model.UserViewModel.GitHubName);
            Assert.NotNull(model.Owners);
            Assert.Single(model.Owners);
            Assert.Equal("/owner-1", model.GetDashContext());

            _mockRepoSettingsStore.Verify(m => m.GetRepoSettingsForOwnerAsync(ownerId), Times.Once);
        }

        [Fact]
        public void ViewComponentLoadsPrivateWithOwnerIdandRepoId()
        {
            var userName = "owner-1";
            var ownerId = "owner-1";
            var repoId = "repo-1";
            var area = "summary";

            var vc = new DashboardMenuViewComponent(_mockRepoSettingsStore.Object);
            // Set user
            vc.ViewComponentContext = GetViewContext(userName);

            var asyncResult = vc.InvokeAsync(ownerId, repoId, area);

            Assert.NotNull(asyncResult.Result);

            var result = asyncResult.Result as ViewViewComponentResult;
            Assert.NotNull(result);
            var model = result.ViewData?.Model as DashboardMenuViewModel;
            Assert.NotNull(model);

            Assert.Equal(ownerId, model.SelectedOwnerId);
            Assert.Equal(area, model.ActiveArea);
            Assert.Equal(repoId, model.SelectedRepoId);
            Assert.NotNull(model.UserViewModel);
            Assert.Equal(userName, model.UserViewModel.GitHubName);
            Assert.NotNull(model.Owners);
            Assert.Single(model.Owners);
            Assert.Equal("/owner-1/repo-1", model.GetDashContext());

            _mockRepoSettingsStore.Verify(m => m.GetRepoSettingsForOwnerAsync(ownerId), Times.Once);
        }
    }
}