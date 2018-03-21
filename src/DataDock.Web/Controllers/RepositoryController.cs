﻿using System;
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
    [Authorize]
    [ServiceFilter(typeof(AccountExistsFilter))]
    public class RepositoryController : DashboardBaseController
    {
        private readonly IRepoSettingsRepository _repoSettingsRepository;

        public RepositoryController(IRepoSettingsRepository repoSettingsRepository)
        {
            _repoSettingsRepository = repoSettingsRepository;
        }

        /// <summary>
        /// User or Org summary of data uploads to a partcular repo
        /// Viewable by public and other DataDock users as well as authorized users
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="repoId"></param>
        /// <returns></returns>
        public async Task<IActionResult> Index(string ownerId, string repoId)
        {
            this.DashboardViewModel.Area = "summary";
            DashboardViewModel.Title = string.Format("{0} > {1} Summary", DashboardViewModel.SelectedOwnerId, DashboardViewModel.SelectedRepoId);
            return View("Dashboard/Index", this.DashboardViewModel);
        }

        /// <summary>
        /// User or Org dataset uploads to a partcular repo
        /// Viewable by public and other DataDock users as well as authorized users
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="repoId"></param>
        /// <returns></returns>
        public async Task<IActionResult> Datasets(string ownerId, string repoId)
        {
            this.DashboardViewModel.Area = "datasets";
            DashboardViewModel.Title = string.Format("{0} > {1} Datasets", DashboardViewModel.SelectedOwnerId, DashboardViewModel.SelectedRepoId);
            return View("Dashboard/Datasets", this.DashboardViewModel);
        }

        /// <summary>
        /// User or Org template library for a partcular repo
        /// Viewable by authorized users only
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="repoId"></param>
        /// <returns></returns>
        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        [ServiceFilter(typeof(OwnerAdminAuthFilter))]
        public async Task<IActionResult> Library(string ownerId, string repoId)
        {
            this.DashboardViewModel.Area = "library";
            DashboardViewModel.Title = string.Format("{0} > {1} Template Library", DashboardViewModel.SelectedOwnerId, DashboardViewModel.SelectedRepoId);
            return View("Dashboard/Library", this.DashboardViewModel);
        }

        /// <summary>
        /// Add data to an org or user github to a partcular repo
        /// Viewable by authorized users only
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="repoId"></param>
        /// <returns></returns>
        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        [ServiceFilter(typeof(OwnerAdminAuthFilter))]
        public async Task<IActionResult> Import(string ownerId, string repoId)
        {
            this.DashboardViewModel.Area = "import";
            DashboardViewModel.Title = string.Format("{0} > {1} Add Data", DashboardViewModel.SelectedOwnerId, DashboardViewModel.SelectedRepoId);
            return View("Dashboard/Import", this.DashboardViewModel);
        }

        /// <summary>
        /// job history list for a partcular repo
        /// Viewable by authorized users only
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="repoId"></param>
        /// <returns></returns>
        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        [ServiceFilter(typeof(OwnerAdminAuthFilter))]
        public async Task<IActionResult> Jobs(string ownerId, string repoId)
        {
            this.DashboardViewModel.Area = "jobs";
            DashboardViewModel.Title = string.Format("{0} > {1} Job History", DashboardViewModel.SelectedOwnerId, DashboardViewModel.SelectedRepoId);
            return View("Dashboard/Jobs", this.DashboardViewModel);
        }

        /// <summary>
        /// org/user settings for a partcular repo
        /// Viewable by authorized users only
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="repoId"></param>
        /// <returns></returns>
        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        [ServiceFilter(typeof(OwnerAdminAuthFilter))]
        public async Task<IActionResult> Settings(string ownerId, string repoId)
        {
            this.DashboardViewModel.Area = "settings";
            DashboardViewModel.Title = string.Format("{0} > {1} Settings", DashboardViewModel.SelectedOwnerId, DashboardViewModel.SelectedRepoId);
            return View("Settings", this.DashboardViewModel);
        }

        [HttpPost]
        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        [ServiceFilter(typeof(OwnerAdminAuthFilter))]
        public async Task<IActionResult> Settings(string ownerId, string repoId, RepoSettingsViewModel settingsViewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    settingsViewModel.LastModified = DateTime.UtcNow;
                    settingsViewModel.LastModifiedBy = User.Identity.Name;
                    var repoSettings = settingsViewModel.AsRepoSettings();
                    await _repoSettingsRepository.CreateOrUpdateRepoSettingsAsync(repoSettings);
                    ViewBag.StatusMessage = GetSettingsStatusMessage(ManageMessageId.ChangeSettingSuccess);
                    TempData["ModelState"] = null;
                }
                catch (Exception e)
                {
                    ViewBag.StatusMessage = GetSettingsStatusMessage(ManageMessageId.Error);
                    Log.Error(e, "Error updating repo settings for '{0}/{1}'", ownerId, repoId);
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
            DashboardViewModel.Title = string.Format("{0} Settings", DashboardViewModel.SelectedOwnerId, DashboardViewModel.SelectedRepoId);
            return View("Settings", this.DashboardViewModel);
        }
    }
}