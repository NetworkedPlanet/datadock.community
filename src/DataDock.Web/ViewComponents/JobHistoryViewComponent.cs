using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using DataDock.Common.Stores;
using DataDock.Web.Models;

namespace DataDock.Web.ViewComponents
{
    [ViewComponent(Name = "JobHistory")]
    public class JobHistoryViewComponent : ViewComponent
    {
        private readonly IJobStore _jobStore;
        public JobHistoryViewComponent(IJobStore jobStore)
        {
            _jobStore = jobStore;
        }

        public async Task<IViewComponentResult> InvokeAsync(string selectedOwnerId, string selectedRepoId)
        {
            try
            {
                if (string.IsNullOrEmpty(selectedOwnerId)) return View("Empty");

                if (string.IsNullOrEmpty(selectedRepoId))
                {
                    var jobList = await GetOwnerJobHistory(selectedOwnerId);
                    return View(jobList);
                }
                var repoJobList = await GetRepoJobHistory(selectedOwnerId, selectedRepoId);
                return View(repoJobList);
            }
            catch (Exception e)
            {
                return View("Error", e);
            }
            
        }

        private async Task<List<JobHistoryViewModel>> GetOwnerJobHistory(string selectedOwnerId)
        {
            try
            {
                var jobs = await _jobStore.GetJobsForOwner(selectedOwnerId);
                var jobHistoriesHistoryViewModels = jobs.Select(j => new JobHistoryViewModel(j)).ToList();
                return jobHistoriesHistoryViewModels;
            }
            catch (JobNotFoundException jnf)
            {
                return new List<JobHistoryViewModel>();
            }
        }

        private async Task<List<JobHistoryViewModel>> GetRepoJobHistory(string selectedOwnerId, string selectedRepoId)
        {
            try
            {
                var jobs = await _jobStore.GetJobsForRepository(selectedOwnerId, selectedRepoId);
                var jobHistoriesHistoryViewModels = jobs.Select(j => new JobHistoryViewModel(j)).ToList();
                return jobHistoriesHistoryViewModels;
            }
            catch (JobNotFoundException jnf)
            {
                return new List<JobHistoryViewModel>();
            }
        }
    }
}
