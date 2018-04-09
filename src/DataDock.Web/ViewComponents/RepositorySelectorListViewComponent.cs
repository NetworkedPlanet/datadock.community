using DataDock.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Octokit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataDock.Web.ViewModels;

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

        public async Task<IViewComponentResult> InvokeAsync(string selectedOwnerId, string display = "dropdown")
        {
            try
            {
                if (string.IsNullOrEmpty(selectedOwnerId)) return View("Empty");
                if (User?.Identity == null || !User.Identity.IsAuthenticated) return View("Empty");

                var repos = await GetRepositoriesForOwner(selectedOwnerId);
                switch (display)
                {
                    case "link-list":
                        return View("DividedList", repos);
                    default:
                        return View(repos);
                }
            }
            catch (Exception e)
            {
                return View("Error", e);
            }
        }

        public async Task<List<RepositoryInfoViewModel>> GetRepositoriesForOwner(string ownerId)
        {
            var repoInfos = new List<RepositoryInfoViewModel>();
            var allRepositories = await _gitHubApiService.GetRepositoryListForOwnerAsync(User.Identity, ownerId);
            foreach (var r in allRepositories)
            {
                repoInfos.Add(new RepositoryInfoViewModel(r));
            }
            return repoInfos;
        }
    }
}
