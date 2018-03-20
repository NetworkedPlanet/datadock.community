using DataDock.Web.Auth;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DataDock.Web.Controllers
{
    [Authorize]
    [ServiceFilter(typeof(AccountExistsFilter))]
    public class OwnerController : DashboardBaseController
    {
        public async Task<IActionResult> Index(string ownerId = "")
        {
            // dash view model
            var dvm = new DashboardViewModel
            {
                SelectedOwnerId = RequestedOwnerId,
                SelectedRepoId = RequestedRepoId
            };
            return View(dvm);
        }

        public async Task<IActionResult> Jobs(string ownerId = "")
        {
            // dash view model
            var dvm = new DashboardViewModel
            {
                SelectedOwnerId = RequestedOwnerId,
                SelectedRepoId = RequestedRepoId
            };
            return View(dvm);
        }
    }
}