using System;
using System.Collections.Generic;
using Nest;

namespace Datadock.Common.Models
{
    [ElasticsearchType(Name="job", IdProperty = "JobId")]
    public class JobInfo
    {
        public JobInfo(JobRequestInfo req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (string.IsNullOrEmpty(req.UserId))
                throw new ArgumentException("UserId field must be a non-null, non-empty string", nameof(req));
            if (string.IsNullOrEmpty(req.OwnerId))
                throw new ArgumentException("OwnerId field must be a non-null, non-empty string", nameof(req));
            if (string.IsNullOrEmpty(req.RepositoryId))
                throw new ArgumentException("RepositoryId field must be a non-null, non-empty string", nameof(req));

            JobId = Guid.NewGuid().ToString("N");
            UserId = req.UserId;
            OwnerId = req.OwnerId;
            RepositoryId = req.RepositoryId;
            JobType = req.JobType;
            Parameters = req.Parameters == null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string>(req.Parameters);
            QueuedAt = DateTime.UtcNow;
            CurrentStatus = JobStatus.Queued;
        }

        /// <summary>
        /// The unique identifier for this job
        /// </summary>
        [Keyword]
        public string JobId { get; set; }

        /// <summary>
        /// The identifier for the user who started the job
        /// </summary>
        [Keyword]
        public string UserId { get; set; }

        /// <summary>
        /// The identifier of the owner of the repository that the job will operate on
        /// </summary>
        [Keyword]
        public string OwnerId { get; set; }

        /// <summary>
        /// The repository that this job will operate on
        /// </summary>
        [Keyword]
        public string RepositoryId { get; set; }

        /// <summary>
        /// The type of job
        /// </summary>
        [Number(NumberType.Integer)]
        public JobType JobType { get; set; }

        /// <summary>
        /// The full name of the Git repository to be updated in the format {owner-login}/{repo-name}
        /// </summary>
        [Keyword(Index = false, Store=true)]
        public string GitRepositoryFullName { get; set; }

        /// <summary>
        /// The URL to use when cloning the Git repository
        /// </summary>
        [Keyword(Index = false, Store=true)]
        public string GitRepositoryUrl { get; set; }
        /// <summary>
        /// The current status of this job
        /// </summary>
        [Number(NumberType.Integer)]
        public JobStatus CurrentStatus { get; set; }

        /// <summary>
        /// The timestamp for the date/time when this job was queued.
        /// </summary>
        /// <remarks>This field is indexed and so can be used for queries. It is set by setting the <see cref="QueuedAt"/> property</remarks>
        [Number(NumberType.Long)]
        public long QueuedTimestamp { get; private set; }

        private DateTime _queuedAt;
        /// <summary>
        /// The date/time when the job was queued.
        /// </summary>
        /// <remarks>This field is not indexed. It is just stored for passing through to the UI.</remarks>
        [Date(Index = false, Store = true)]
        public DateTime QueuedAt
        {
            get => _queuedAt;
            set
            {
                _queuedAt = value;
                QueuedTimestamp = value.Ticks;
            }
        }

        /// <summary>
        /// The date/time when a worker started working on the job.
        /// </summary>
        /// <remarks>This field is not indexed. It is just stored for passing through to the UI</remarks>
        [Date(Index = false, Store = true)]
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// The date/time when the job was completed
        /// </summary>
        /// <remarks>This field is not indexed. It is just stored for passing through to the UI</remarks>
        [Date(Index = false, Store = true)]
        public DateTime CompletedAt { get; set; }

        /// <summary>
        /// The timestamp of the date/time when the lock on this job was last refreshed
        /// </summary>
        [Number(NumberType.Long)]
        public long RefreshedTimestamp { get; set; }

        /// <summary>
        /// Additional job-specific parameters
        /// </summary>
        [Object]
        public Dictionary<string, string> Parameters { get; set; }
    }
}
