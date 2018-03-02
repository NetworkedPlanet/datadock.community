using System.Collections.Generic;
using Nest;

namespace Datadock.Common.Models
{
    public class JobRequestInfo
    {
        /// <summary>
        /// The identifier for the user who started the job
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// The identifier of the owner of the repository that the job will operate on
        /// </summary>
        public string OwnerId { get; set; }

        /// <summary>
        /// The repository that this job will operate on
        /// </summary>
        public string RepositoryId { get; set; }

        /// <summary>
        /// The type of job
        /// </summary>
        public JobType JobType { get; set; }

        /// <summary>
        /// Additional job-specific parameters
        /// </summary>
        [Object]
        public Dictionary<string, string> Parameters { get; set; }

    }
}