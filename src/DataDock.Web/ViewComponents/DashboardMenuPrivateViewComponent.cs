using System.Linq;
using System.Security.Claims;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using DataDock.Web.Models;

namespace DataDock.Web.ViewComponents
{
    [ViewComponent(Name = "DashboardMenuPrivate")]
    public class DashboardMenuPrivateViewComponent : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(string selectedOwnerId, string selectedRepoId)
        {
            if (!User.Identity.IsAuthenticated) return View("Blank");
            
            // user view model
            var uvm = new UserViewModel();
            uvm.Populate(User.Identity as ClaimsIdentity);
                
            // dash view model
            var dvm = new DashboardMenuPrivateViewModel
            {
                SelectedOwnerId = selectedOwnerId,
                SelectedRepoId = selectedRepoId
            };
            dvm.Owners.Add(uvm.UserOwner);
            dvm.Owners.AddRange(uvm.Organisations);
            dvm.SelectedOwnerAvatarUrl = dvm.Owners.FirstOrDefault(o => o.OwnerId.Equals(selectedOwnerId))?.AvatarUrl;
            return View(dvm);

        }
    }
}
