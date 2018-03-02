﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Datadock.Common.Models;

namespace Datadock.Common.Repositories
{
    public interface IJobRepository
    {
        /// <summary>
        /// Create a new job and add it to the queue
        /// </summary>
        /// <param name="jobDescription"></param>
        /// <returns></returns>
        Task<string> SubmitJobAsync(JobRequestInfo jobDescription);

        /// <summary>
        /// Retrieve the details for the specified job
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        Task<JobInfo> GetJobInfoAsync(string jobId);

        /// <summary>
        /// Update the details for the specified job
        /// </summary>
        /// <param name="updatedJobInfo"></param>
        /// <returns></returns>
        Task<bool> UpdateJobInfoAsync(JobInfo updatedJobInfo);

        /// <summary>
        /// Retrieve all jobs created by the specified user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<IEnumerable<JobInfo>> GetJobsForUser(string userId, int skip=0, int take=20);

        /// <summary>
        /// Retrieve all jobs that operate on repositories owned by the specified GitHub owner
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        Task<IEnumerable<JobInfo>> GetJobsForOwner(string ownerId, int skip=0, int take=20);


        /// <summary>
        /// Retrieve all jobs that operate on the specified repository
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="repositoryId"></param>
        /// <returns></returns>
        Task<IEnumerable<JobInfo>> GetJobsForRepository(string ownerId, string repositoryId, int skip=0, int take=20);

        /// <summary>
        /// Get the next job that is available to be processed
        /// </summary>
        /// <returns></returns>
        Task<JobInfo> GetNextJob();

    }
}
