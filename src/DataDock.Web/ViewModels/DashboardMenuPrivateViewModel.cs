using System;
using DataDock.Web.Models;
using System.Collections.Generic;

namespace DataDock.Web.ViewModels
{
    /// <summary>
    /// The dashboard menu displays a dropdown list of owners that the user has access rights to (set during login in claims)
    /// It also displays links to repositories / datasets / templates / add data and settings for that owner
    /// </summary>
    public class DashboardMenuPrivateViewModel
    {
        public string SelectedOwnerId { get; set; }
        public string SelectedOwnerAvatarUrl { get; set; }
        
        public string SelectedRepoId { get; set; }

        public string ActiveArea { get; set; }

        public List<Owner> Owners { get; set; }

        public DashboardMenuPrivateViewModel()
        {
            Owners = new List<Owner>();
        }

        /// <summary>
        /// return either {ownerId} or {ownerId}/{repoId} for use in link URLs
        /// </summary>
        /// <returns></returns>
        public string GetDashContext()
        {
            return string.IsNullOrEmpty(SelectedRepoId) ? SelectedOwnerId : string.Format("{0}/{1}", SelectedOwnerId, SelectedRepoId);
        }

        /// <summary>
        /// return CSS class name(s) to be used on menu items
        /// </summary>
        /// <param name="area"></param>
        /// <returns></returns>
        public string AreaIsActive(string area)
        {
            if (string.IsNullOrEmpty(area)) return string.Empty;
            return area.Equals(ActiveArea, StringComparison.InvariantCultureIgnoreCase) ? "active" : string.Empty;
        }
    }
}
