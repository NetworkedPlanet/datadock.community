using Datadock.Common.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace DataDock.Web.ViewModels
{
    public class SettingsViewModel : DashboardViewModel
    {
        public string OwnerId { get; set; }
        public bool OwnerIsOrg { get; set; }

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
        
        [Display(Name = "Portal Search Buttons")]
        public string SearchButtons { get; set; }


        /*
         * Last Modified
         */
        public DateTime LastModified { get; set; }
        public string LastModifiedBy { get; set; }
        
    }
}
