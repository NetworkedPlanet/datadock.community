﻿using DataDock.Common.Stores;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task<IViewComponentResult> InvokeAsync(string selectedOwnerId, string selectedRepoId, string currentJobId = "")
        {
            try
            {
                if (string.IsNullOrEmpty(selectedOwnerId)) return View("Empty");

                if (string.IsNullOrEmpty(selectedRepoId))
                {
                    var jobList = await GetOwnerJobHistory(selectedOwnerId, currentJobId);
                    return View(jobList);
                }
                var repoJobList = await GetRepoJobHistory(selectedOwnerId, selectedRepoId, currentJobId);
                return View(repoJobList);
            }
            catch (Exception e)
            {
                return View("Error", e);
            }
            
        }

        private async Task<List<JobHistoryViewModel>> GetOwnerJobHistory(string selectedOwnerId, string currentJobId = "")
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

        private async Task<List<JobHistoryViewModel>> GetRepoJobHistory(string selectedOwnerId, string selectedRepoId, string currentJobId = "")
        {
            try
            {
                var jobs = await _jobStore.GetJobsForRepository(selectedOwnerId, selectedRepoId);
                var jobHistoriesHistoryViewModels = jobs.Select(j => new JobHistoryViewModel(j)).ToList();
                if (!string.IsNullOrEmpty(currentJobId))
                {
                    //check current job has been loaded
                    var currentJob = jobs.FirstOrDefault(j => j.JobId.Equals(currentJobId));
                    if (currentJob == null)
                    {
                        currentJob = await _jobStore.GetJobInfoAsync(currentJobId);
                        var cjvm = new JobHistoryViewModel(currentJob);
                        jobHistoriesHistoryViewModels.Add(cjvm);
                    }
                }
                return jobHistoriesHistoryViewModels;
            }
            catch (JobNotFoundException jnf)
            {
                return new List<JobHistoryViewModel>();
            }
        }
    }
}
