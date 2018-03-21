using System;
using DataDock.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Datadock.Common.Repositories;
using DataDock.Web.Models;
using DataDock.Web.ViewModels;
using Serilog;

namespace DataDock.Web.Controllers
{
    
    public class OwnerController : DashboardBaseController
    {
        private readonly IOwnerSettingsRepository _ownerSettingsRepository;

        public OwnerController(IOwnerSettingsRepository ownerSettingsRepository)
        {
            _ownerSettingsRepository = ownerSettingsRepository;
        }

        /// <summary>
        /// User or Org summary of data uploads
        /// Viewable by public and other DataDock users as well as authorized users
        /// </summary>
        /// <param name="ownerId"></param>
        /// <returns></returns>
        public async Task<IActionResult> Index(string ownerId = "")
        {
            this.DashboardViewModel.Area = "summary";
             DashboardViewModel.Title = string.Format("{0} Summary", DashboardViewModel.SelectedOwnerId);
            return View("Dashboard/Index", this.DashboardViewModel);
        }

        /// <summary>
        /// User or Org repository list
        /// Viewable by public and other DataDock users as well as authorized users
        /// </summary>
        /// <param name="ownerId"></param>
        /// <returns></returns>
        public async Task<IActionResult> Repositories(string ownerId = "")
        {
            this.DashboardViewModel.Area = "repositories";
            DashboardViewModel.Title = string.Format("{0} Repos", DashboardViewModel.SelectedOwnerId);
            return View("Dashboard/Repositories", this.DashboardViewModel);
        }

        /// <summary>
        /// User or Org dataset uploads
        /// Viewable by public and other DataDock users as well as authorized users
        /// </summary>
        /// <param name="ownerId"></param>
        /// <returns></returns>
        public async Task<IActionResult> Datasets(string ownerId = "")
        {
            this.DashboardViewModel.Area = "datasets";
            DashboardViewModel.Title = string.Format("{0} Datasets", DashboardViewModel.SelectedOwnerId);
            return View("Dashboard/Datasets", this.DashboardViewModel);
        }

        /// <summary>
        /// User or Org template library
        /// Viewable by authorized users only
        /// </summary>
        /// <param name="ownerId"></param>
        /// <returns></returns>
        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        [ServiceFilter(typeof(OwnerAdminAuthFilter))]
        public async Task<IActionResult> Library(string ownerId = "")
        {
            this.DashboardViewModel.Area = "library";
            DashboardViewModel.Title = string.Format("{0} Template Library", DashboardViewModel.SelectedOwnerId);
            return View("Dashboard/Library", this.DashboardViewModel);
        }

        /// <summary>
        /// Add data to an org or user github
        /// Viewable by authorized users only
        /// </summary>
        /// <param name="ownerId"></param>
        /// <returns></returns>
        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        [ServiceFilter(typeof(OwnerAdminAuthFilter))]
        public async Task<IActionResult> Import(string ownerId = "")
        {
            this.DashboardViewModel.Area = "import";
            DashboardViewModel.Title = string.Format("{0} Add Data", DashboardViewModel.SelectedOwnerId);
            return View("Dashboard/Import", this.DashboardViewModel);
        }

        /// <summary>
        /// job history list
        /// Viewable by authorized users only
        /// </summary>
        /// <param name="ownerId"></param>
        /// <returns></returns>
        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        [ServiceFilter(typeof(OwnerAdminAuthFilter))]
        public async Task<IActionResult> Jobs(string ownerId = "")
        {
            this.DashboardViewModel.Area = "jobs";
            DashboardViewModel.Title = string.Format("{0} Job History", DashboardViewModel.SelectedOwnerId);
            return View("Dashboard/Jobs", this.DashboardViewModel);
        }

        /// <summary>
        /// org/user settings
        /// Viewable by authorized users only
        /// </summary>
        /// <param name="ownerId"></param>
        /// <returns></returns>
        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        [ServiceFilter(typeof(OwnerAdminAuthFilter))]
        public async Task<IActionResult> Settings(string ownerId = "")
        {
            ViewBag.StatusMessage = GetSettingsStatusMessage();
            this.DashboardViewModel.Area = "settings";
            DashboardViewModel.Title = string.Format("{0} Settings", DashboardViewModel.SelectedOwnerId);
            return View("Settings", this.DashboardViewModel);
        }

        [HttpPost]
        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        [ServiceFilter(typeof(OwnerAdminAuthFilter))]
        public async Task<IActionResult> Settings(string ownerId, OwnerSettingsViewModel settingsViewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    settingsViewModel.LastModified = DateTime.UtcNow;
                    settingsViewModel.LastModifiedBy = User.Identity.Name;
                    var ownerSettings = settingsViewModel.AsOwnerSettings();
                    await _ownerSettingsRepository.CreateOrUpdateOwnerSettingsAsync(ownerSettings);
                    ViewBag.StatusMessage = GetSettingsStatusMessage(ManageMessageId.ChangeSettingSuccess);
                    TempData["ModelState"] = null;
                }
                catch (Exception e)
                {
                    ViewBag.StatusMessage = GetSettingsStatusMessage(ManageMessageId.Error);
                    Log.Error(e, "Error updating owner settings for '{0}'", ownerId);
                    throw;
                }
            }
            else
            {
                // pass errors to the ViewComponent
                ViewBag.StatusMessage = GetSettingsStatusMessage(ManageMessageId.ValidationError);
                TempData["ModelState"] = ModelState;
                TempData["ViewModel"] = settingsViewModel;
            }

            this.DashboardViewModel.Area = "settings";
            DashboardViewModel.Title = string.Format("{0} Settings", DashboardViewModel.SelectedOwnerId);
            return View("Settings", this.DashboardViewModel);
        }
    }
}