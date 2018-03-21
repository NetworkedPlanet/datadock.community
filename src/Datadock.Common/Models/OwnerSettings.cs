using System;

namespace Datadock.Common.Models
{
    public class OwnerSettings
    {
        public string OwnerId { get; set; }

        public bool IsOrg { get; set; }

        /// <summary>
        /// The default publisher info to use on all repositories under this owner 
        /// (unless overwritten by a publisher setting at the repository level 
        /// or on a single dataset upload
        /// </summary>
        public ContactInfo DefaultPublisher { get; set; }

        /// <summary>
        /// Date and time that the settings were last changed
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// UserId of the person who last changed the settings
        /// </summary>
        public string LastModifiedBy { get; set; }

        public bool DisplayGitHubIssuesLink { get; set; }

        public string TwitterHandle { get; set; }

        public string LinkedInProfileUrl { get; set; }

        public bool DisplayGitHubAvatar { get; set; }

        public bool DisplayGitHubDescription { get; set; }

        public bool DisplayDataDockLink { get; set; }

        public bool DisplayGitHubLocation { get; set; }

        public bool DisplayGitHubBlogUrl { get; set; }

        public string SearchButtons { get; set; }

    }
}
