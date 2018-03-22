using System;
using System.Linq;
using System.Security.Claims;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using DataDock.Web.Auth;

namespace DataDock.Web.ViewComponents
{
    [ViewComponent(Name = "DashboardMenu")]
    public class DashboardMenuViewComponent : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(string selectedOwnerId, string selectedRepoId, string area)
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated || !ClaimsHelper.OwnerExistsInUserClaims(User.Identity as ClaimsIdentity, selectedOwnerId))
            {
                // dash view model
                var publicDash = new DashboardMenuViewModel
                {
                    SelectedOwnerId = selectedOwnerId,
                    SelectedRepoId = selectedRepoId,
                    ActiveArea = area
                };
                // TODO: avatar URL cannot be retrieved from the user claims, so needs to be retrieved from data storage / cache
                return View("Public", publicDash);
            }
            
            // user view model
            var uvm = new UserViewModel();
            uvm.Populate(User.Identity as ClaimsIdentity);
                
            // dash view model
            var dvm = new DashboardMenuViewModel
            {
                SelectedOwnerId = selectedOwnerId,
                SelectedRepoId = selectedRepoId,
                UserViewModel = uvm,
                ActiveArea = area
            };
            dvm.Owners.Add(uvm.UserOwner);
            dvm.Owners.AddRange(uvm.Organisations);
            dvm.SelectedOwnerAvatarUrl = dvm.Owners.FirstOrDefault(o => o.OwnerId.Equals(selectedOwnerId, StringComparison.InvariantCultureIgnoreCase))?.AvatarUrl;
            return View(dvm);

        }
    }
}
