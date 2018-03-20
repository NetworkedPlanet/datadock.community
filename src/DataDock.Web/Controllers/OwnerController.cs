using DataDock.Web.Auth;
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
            this.DashboardViewModel.Area = "summary";
            return View(this.DashboardViewModel);
        }

        public async Task<IActionResult> Repositories(string ownerId = "")
        {
            this.DashboardViewModel.Area = "repositories";
            return View(this.DashboardViewModel);
        }

        public async Task<IActionResult> Datasets(string ownerId = "")
        {
            this.DashboardViewModel.Area = "datasets";
            return View(this.DashboardViewModel);
        }

        public async Task<IActionResult> Library(string ownerId = "")
        {
            this.DashboardViewModel.Area = "library";
            return View(this.DashboardViewModel);
        }

        public async Task<IActionResult> Jobs(string ownerId = "")
        {
            this.DashboardViewModel.Area = "jobs";
            return View(this.DashboardViewModel);
        }

        public async Task<IActionResult> Settings(string ownerId = "")
        {
            this.DashboardViewModel.Area = "settings";
            return View(this.DashboardViewModel);
        }
    }
}