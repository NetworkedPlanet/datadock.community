using Datadock.Common.Models;
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace DataDock.Web.ViewModels
{
    public class UserSettingsViewModel : DashboardViewModel
    {
        [HiddenInput]
        public string UserId { get; set; }
        
        /*
        * Last Modified
        */
        public DateTime LastModified { get; set; }
        public string LastModifiedBy { get; set; }

        public UserSettingsViewModel()
        {
        }
        public UserSettingsViewModel(UserSettings settings)
        {
            UserId = settings.UserId;
            LastModified = settings.LastModified;
            LastModifiedBy = settings.LastModifiedBy;
        }

        public UserSettings AsUserSettings()
        {
            return new UserSettings
            {
                UserId = this.UserId,
                LastModified = this.LastModified,
                LastModifiedBy = this.LastModifiedBy
            };

        }
    }

    public class DeleteAccountViewModel
    {
        [Range(typeof(bool), "true", "true", ErrorMessage = "You must confirm before you can delete your account.")]
        [Display(Name = "Yes, I want to delete my account permanently.")]
        public bool Confirm { get; set; }
    }
    
}
