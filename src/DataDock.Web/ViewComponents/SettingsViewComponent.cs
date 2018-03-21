using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Datadock.Common.Repositories;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Serilog;

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
                    var osvm = await GetOwnerSettingsViewModel(selectedOwnerId);
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

        private async Task<OwnerSettingsViewModel> GetOwnerSettingsViewModel(string ownerId)
        {
            if (string.IsNullOrEmpty(ownerId)) return null;
            try
            {
                var os = await _ownerSettingsRepository.GetOwnerSettingsAsync(ownerId);
                var osvm = new OwnerSettingsViewModel(os);
                return osvm;
            }
            catch (OwnerSettingsNotFoundException notFound)
            {
                Log.Debug("No owner settings found for owner '{0}'", ownerId);
                return new OwnerSettingsViewModel {OwnerId = ownerId};
            }
            catch (Exception e)
            {
                Log.Error(e, "Error retrieving owner settings with owner id '{0}'", ownerId);
                throw;
            }
        }
    }
}
