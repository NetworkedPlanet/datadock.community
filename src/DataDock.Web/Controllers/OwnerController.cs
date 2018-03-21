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
             DashboardViewModel.Title = string.Format("{0} Summary", DashboardViewModel.SelectedOwnerId);
            return View("Dashboard/Index", this.DashboardViewModel);
        }

        public async Task<IActionResult> Repositories(string ownerId = "")
        {
            this.DashboardViewModel.Area = "repositories";
            DashboardViewModel.Title = string.Format("{0} Repos", DashboardViewModel.SelectedOwnerId);
            return View("Dashboard/Repositories", this.DashboardViewModel);
        }

        public async Task<IActionResult> Datasets(string ownerId = "")
        {
            this.DashboardViewModel.Area = "datasets";
            DashboardViewModel.Title = string.Format("{0} Datasets", DashboardViewModel.SelectedOwnerId);
            return View("Dashboard/Datasets", this.DashboardViewModel);
        }

        public async Task<IActionResult> Library(string ownerId = "")
        {
            this.DashboardViewModel.Area = "library";
            DashboardViewModel.Title = string.Format("{0} Template Library", DashboardViewModel.SelectedOwnerId);
            return View("Dashboard/Library", this.DashboardViewModel);
        }

        public async Task<IActionResult> Import(string ownerId = "")
        {
            this.DashboardViewModel.Area = "import";
            DashboardViewModel.Title = string.Format("{0} Add Data", DashboardViewModel.SelectedOwnerId);
            return View("Dashboard/Import", this.DashboardViewModel);
        }

        public async Task<IActionResult> Jobs(string ownerId = "")
        {
            this.DashboardViewModel.Area = "jobs";
            DashboardViewModel.Title = string.Format("{0} Job History", DashboardViewModel.SelectedOwnerId);
            return View("Dashboard/Jobs", this.DashboardViewModel);
        }

        public async Task<IActionResult> Settings(string ownerId = "")
        {
            this.DashboardViewModel.Area = "settings";
            DashboardViewModel.Title = string.Format("{0} Settings", DashboardViewModel.SelectedOwnerId);
            return View("Dashboard/Settings", this.DashboardViewModel);
        }
    }
}