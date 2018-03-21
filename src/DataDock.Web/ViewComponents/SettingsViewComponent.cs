using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Datadock.Common.Repositories;
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

            if (string.IsNullOrEmpty(selectedRepoId))
            {
                return View("Owner");
            }
            return View("Repo");
        }
    }
}
