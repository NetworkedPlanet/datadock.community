using System;

namespace Datadock.Common.Models
{
    public class RepoSettings
    {
        /// <summary>
        /// The repository owner
        /// </summary>
        public string OwnerId { get; set; }

        public bool OwnerIsOrg { get; set; }

        /// <summary>
        /// The DataDock repository ID ({owner-name}/{repo-name})
        /// </summary>
        public string RepositoryId { get; set; }

        /// <summary>
        /// True if this repository is part of an organization's account
        /// </summary>
        public bool IsOrgRepo { get; set; }

        public string CloneUrl { get; set; }

        /// <summary>
        /// GitHub repository name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The link to the avatar image of the repository owner (may be null)
        /// </summary>
        public string OwnerAvatar { get; set; }

        /// <summary>
        /// The Lodlab publisher metadata for this repository
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

        public string SearchButtons { get; set; }

    }
}
