using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Datadock.Common.Repositories;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DataDock.Web.ViewComponents
{
    public class SettingsViewComponent : ViewComponent
    {
        private readonly IOwnerSettingsRepository _ownerSettingsRepository;
        private readonly IRepoSettingsRepository _repoSettingsRepository;
        public SettingsViewComponent(IOwnerSettingsRepository ownerSettingsRepository, IRepoSettingsRepository repoSettingsRepository)
        {
            _ownerSettingsRepository = ownerSettingsRepository;
            _repoSettingsRepository = repoSettingsRepository;
        }

        public async Task<IViewComponentResult> InvokeAsync(string selectedOwnerId, string selectedRepoId)
        {
            if (string.IsNullOrEmpty(selectedOwnerId)) return View("Empty");

            try
            {
                if (string.IsNullOrEmpty(selectedRepoId))
                {
                    var osvm = new OwnerSettingsViewModel {OwnerId = selectedOwnerId};
                    return View("Owner", osvm);
                }

                var rsvm = new RepoSettingsViewModel
                {
                    OwnerId = selectedOwnerId,
                    RepoId = selectedRepoId,
                    OwnerRepositoryId = string.Format("{0}/{1}", selectedOwnerId, selectedRepoId)
                };
                return View("Repo", rsvm);
            }
            catch (Exception e)
            {
                return View("Error", e);
            }
           
        }
    }
}
