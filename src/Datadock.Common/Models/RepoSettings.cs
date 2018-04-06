using System;
using Nest;

namespace Datadock.Common.Models
{
    [ElasticsearchType(Name = "reposettings")]
    public class RepoSettings
    {
        /// <summary>
        /// The repository owner
        /// </summary>
        public string OwnerId { get; set; }

        public bool OwnerIsOrg { get; set; }

        /// <summary>
        /// The DataDock repository ID
        /// </summary>
        public string RepoId { get; set; }
        
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
