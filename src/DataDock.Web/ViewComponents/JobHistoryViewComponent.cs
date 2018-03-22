using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Datadock.Common.Repositories;
using DataDock.Web.Models;

namespace DataDock.Web.ViewComponents
{
    [ViewComponent(Name = "JobHistory")]
    public class JobHistoryViewComponent : ViewComponent
    {
        private readonly IJobRepository _jobRepository;
        public JobHistoryViewComponent(IJobRepository jobRepository)
        {
            _jobRepository = jobRepository;
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
            var jobs = await _jobRepository.GetJobsForOwner(selectedOwnerId);
            var jobHistoriesHistoryViewModels = jobs.Select(j => new JobHistoryViewModel(j)).ToList();
            return jobHistoriesHistoryViewModels;
        }

        private async Task<List<JobHistoryViewModel>> GetRepoJobHistory(string selectedOwnerId, string selectedRepoId)
        {
            var jobs = await _jobRepository.GetJobsForRepository(selectedOwnerId, selectedRepoId);
            var jobHistoriesHistoryViewModels = jobs.Select(j => new JobHistoryViewModel(j)).ToList();
            return jobHistoriesHistoryViewModels;
        }
    }
}
