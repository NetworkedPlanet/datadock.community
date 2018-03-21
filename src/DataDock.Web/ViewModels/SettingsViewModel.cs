using Datadock.Common.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace DataDock.Web.ViewModels
{
    public class SettingsViewModel
    {
        public string OwnerId { get; set; }
        public bool IsOrg { get; set; }
        
        /*
         * Default Publisher
         */
        [Display(Name = "Publisher Type", GroupName = "Publisher", Order = 0)]
        public string DefaultPublisherType { get; set; }

        [Display(Name = "Publisher Name", GroupName = "Publisher", Order = 1)]
        public string DefaultPublisherName { get; set; }

        [Display(Name = "Publisher Email", GroupName = "Publisher", Order = 2)]
        [DataType(DataType.EmailAddress, ErrorMessage = "E-mail is not valid")]
        public string DefaultPublisherEmail { get; set; }

        [Display(Name = "Publisher Website", GroupName = "Publisher", Order = 3)]
        [DataType(DataType.Url, ErrorMessage = "Website address is not valid")]
        public string DefaultPublisherWebsite { get; set; }

        [Display(Name = "Twitter Handle")]
        public string TwitterHandle { get; set; }

        [Display(Name = "LinkedIn Profile URL")]
        [DataType(DataType.Url, ErrorMessage = "Web address is not valid")]
        public string LinkedInProfileUrl { get; set; }


        [Display(Name = "Display Link To DataDock Management Dashboard")]
        public bool DisplayDataDockLink { get; set; }

        [Display(Name = "Display Link To GitHub Issues List")]
        public bool DisplayGitHubIssuesLink { get; set; }

        [Display(Name = "Display GitHub Avatar")]
        public bool DisplayGitHubAvatar { get; set; }

        [Display(Name = "Display Description (as set as GitHub 'Bio')")]
        public bool DisplayGitHubDescription { get; set; }

        [Display(Name = "Display Website URL (as set in GitHub Profile)")]
        public bool DisplayGitHubBlogUrl { get; set; }

        [Display(Name = "Display Location (as set in GitHub Profile)")]
        public bool DisplayGitHubLocation { get; set; }

        [Display(Name = "Portal Search Buttons")]
        public string SearchButtons { get; set; }


        /*
         * Last Modified
         */
        public DateTime LastModified { get; set; }
        public string LastModifiedBy { get; set; }

        public SettingsViewModel() { }

        public SettingsViewModel(OwnerSettings ownerSettings)
        {
            OwnerId = ownerSettings.OwnerId;
            DefaultPublisherName = ownerSettings.DefaultPublisher?.Label;
            DefaultPublisherType = ownerSettings.DefaultPublisher?.Type;
            DefaultPublisherEmail = ownerSettings.DefaultPublisher?.Email;
            DefaultPublisherWebsite = ownerSettings.DefaultPublisher?.Website;
            TwitterHandle = ownerSettings.TwitterHandle;
            LinkedInProfileUrl = ownerSettings.LinkedInProfileUrl;
            DisplayDataDockLink = ownerSettings.DisplayDataDockLink;
            DisplayGitHubIssuesLink = ownerSettings.DisplayGitHubIssuesLink;
            DisplayGitHubAvatar = ownerSettings.DisplayGitHubAvatar;
            DisplayGitHubDescription = ownerSettings.DisplayGitHubDescription;
            DisplayGitHubLocation = ownerSettings.DisplayGitHubLocation;
            DisplayGitHubBlogUrl = ownerSettings.DisplayGitHubBlogUrl;
            SearchButtons = ownerSettings.SearchButtons;
            LastModifiedBy = ownerSettings.LastModifiedBy;
            LastModified = ownerSettings.LastModified;
        }

        public OwnerSettings AsOwnerSettings()
        {
            return new OwnerSettings()
            {
                OwnerId = this.OwnerId,
                DefaultPublisher = new ContactInfo
                {
                    Label = this.DefaultPublisherName,
                    Type = this.DefaultPublisherType,
                    Email = this.DefaultPublisherEmail,
                    Website = this.DefaultPublisherWebsite
                },
                IsOrg = this.IsOrg,
                TwitterHandle = !string.IsNullOrEmpty(this.TwitterHandle) ? this.TwitterHandle.Replace("@", "").Trim() : null,
                LinkedInProfileUrl = this.LinkedInProfileUrl,
                DisplayDataDockLink = this.DisplayDataDockLink,
                DisplayGitHubIssuesLink = this.DisplayGitHubIssuesLink,
                DisplayGitHubAvatar = this.DisplayGitHubAvatar,
                DisplayGitHubDescription = this.DisplayGitHubDescription,
                DisplayGitHubLocation = this.DisplayGitHubLocation,
                DisplayGitHubBlogUrl = this.DisplayGitHubBlogUrl,
                SearchButtons = this.SearchButtons,
                LastModifiedBy = this.LastModifiedBy,
                LastModified = this.LastModified
            };
        }
    }
}
