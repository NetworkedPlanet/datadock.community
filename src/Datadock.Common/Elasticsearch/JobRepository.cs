﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Datadock.Common.Models;
using Datadock.Common.Repositories;
using Nest;

namespace Datadock.Common.Elasticsearch
{
    public class JobRepository : IJobRepository
    {
        private readonly IElasticClient _client;
        public JobRepository(IElasticClient client)
        {
            _client = client;
        }

        public async Task<JobInfo> SubmitImportJobAsync(ImportJobRequestInfo jobDescription)
        {
            var jobInfo = new JobInfo(jobDescription);
            return await SubmitJobAsync(jobInfo);
        }

        public async Task<JobInfo> SubmitDeleteJobAsync(DeleteJobRequestInfo jobRequest)
        {
            var jobInfo = new JobInfo(jobRequest);
            return await SubmitJobAsync(jobInfo);
        }

        public async Task<JobInfo> SubmitSchemaImportJobAsync(SchemaImportJobRequestInfo jobRequest)
        {
            var jobInfo = new JobInfo(jobRequest);
            return await SubmitJobAsync(jobInfo);
        }

        public async Task<JobInfo> SubmitSchemaDeleteJobAsync(SchemaDeleteJobRequestInfo jobRequest)
        {
            var jobInfo = new JobInfo(jobRequest);
            return await SubmitJobAsync(jobInfo);
        }

        private async Task<JobInfo> SubmitJobAsync(JobInfo jobInfo) { 
            var indexResponse = await _client.IndexDocumentAsync<JobInfo>(jobInfo);
            if (!indexResponse.IsValid)
            {
                throw new JobRepositoryException($"Failed to insert new job record: {indexResponse.DebugInformation}");
            }
            return jobInfo;
        }

        public async Task<JobInfo> GetJobInfoAsync(string jobId)
        {
            var getResponse = await _client.GetAsync<JobInfo>(jobId);
            if (getResponse.IsValid) return getResponse.Source;
            if (!getResponse.Found)
            {
                throw new JobNotFoundException(jobId);
            }
            throw new JobRepositoryException($"Failed to retrieve job record for jobId {jobId}: {getResponse.DebugInformation}");
        }

        public async Task UpdateJobInfoAsync(JobInfo updatedJobInfo)
        {
            updatedJobInfo.RefreshedTimestamp = DateTime.UtcNow.Ticks;
            var updateResponse = await _client.IndexDocumentAsync(updatedJobInfo);
            if (!updateResponse.IsValid)
            {
                throw new JobRepositoryException(
                    $"Failed to update job record for jobId {updatedJobInfo.JobId}: {updateResponse.DebugInformation}");
            }
        }

        public Task<IEnumerable<JobInfo>> GetJobsForUser(string userId, int skip = 0, int take = 20)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<JobInfo>> GetJobsForOwner(string ownerId, int skip = 0, int take = 20)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<JobInfo>> GetJobsForRepository(string ownerId, string repositoryId, int skip = 0, int take = 20)
        {
            throw new NotImplementedException();
        }

        public async Task<JobInfo> GetNextJob()
        {
            // TODO: Should make sure that: (a) there aren't any jobs running for the same GitHub repository
            // (b) when we claim the job to work on it, no-one else grabbed it before us (i.e. update with If-Not-Modified)
            var searchResults = await _client.SearchAsync<JobInfo>(s => s
                .Query(q => q.Bool(b => b
                    .Filter(bf => bf
                        .Match(m => m
                            .Field(f => f.CurrentStatus)
                            .Query(JobStatus.Queued.ToString())
                        ))))
                .Sort(sort => sort.Ascending(on => on.QueuedTimestamp)).Take(1));
            if (searchResults.Hits.Any())
            {
                // Attempt to update the job document to mark it as running
                var hit = searchResults.Hits.First();
                var resultVersion = hit.Version;
                var jobInfo = hit.Source;

                jobInfo.RefreshedTimestamp = DateTime.UtcNow.Ticks;
                jobInfo.StartedAt = DateTime.UtcNow;
                jobInfo.CurrentStatus = JobStatus.Running;
                var indexResponse = await _client.IndexAsync(jobInfo, desc => desc
                    .Id(jobInfo.JobId)
                    .Version(resultVersion));
                if (indexResponse.IsValid)
                {
                    return jobInfo;
                }
            }

            return null;
        }
    }

    public class JobNotFoundException : JobRepositoryException
    {
        public JobNotFoundException(string jobId) : base("No job found with id " + jobId) { }
    }

    public class JobRepositoryException : DatadockException
    {
        public JobRepositoryException(string msg) : base(msg) { }
    }
}

