using DataDock.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Octokit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataDock.Web.ViewComponents
{
    [ViewComponent(Name = "RepositorySelectorList")]
    public class RepositorySelectorListViewComponent : ViewComponent
    {
        private readonly IGitHubApiService _gitHubApiService;

        public RepositorySelectorListViewComponent(IGitHubApiService gitHubApiService)
        {
            _gitHubApiService = gitHubApiService;
        }

        public async Task<IViewComponentResult> InvokeAsync(string selectedOwnerId)
        {
            try
            {
                if (string.IsNullOrEmpty(selectedOwnerId)) return View("Empty");
                if (User?.Identity == null || !User.Identity.IsAuthenticated) return View("Empty");

                var repos = await GetRepositoriesForOwner(selectedOwnerId);
                return View(repos);
            }
            catch (Exception e)
            {
                return View("Error", e);
            }
        }

        public async Task<List<Repository>> GetRepositoriesForOwner(string ownerId)
        {
            var allRepositories = await _gitHubApiService.GetRepositoryListForOwnerAsync(User.Identity, ownerId);
            return allRepositories;
        }
    }
}
