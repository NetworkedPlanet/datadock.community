using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Datadock.Common.Stores;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DataDock.Web.ViewComponents
{
    [ViewComponent(Name = "RepositoryList")]
    public class RepositoryListViewComponent : ViewComponent
    {
        private readonly IRepoSettingsStore _repoSettingsStore;

        public RepositoryListViewComponent(IRepoSettingsStore repoSettingsStore)
        {
            _repoSettingsStore = repoSettingsStore;
        }

        public async Task<IViewComponentResult> InvokeAsync(string selectedOwnerId)
        {
            try
            {
                if (string.IsNullOrEmpty(selectedOwnerId)) return View("Empty");

                var repos = await GetOwnerRepoSettings(selectedOwnerId);
                return View(repos);
            }
            catch (Exception e)
            {
                return View("Error", e);
            }
        }

        private async Task<List<RepoSettingsViewModel>> GetOwnerRepoSettings(string selectedOwnerId)
        {
            var repoSettings = await _repoSettingsStore.GetRepoSettingsForOwnerAsync(selectedOwnerId);
            var repoSettingsViewModels = repoSettings.Select(r => new RepoSettingsViewModel(r)).ToList();
            return repoSettingsViewModels;
        }
    }
}
