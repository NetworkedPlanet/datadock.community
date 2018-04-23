using Microsoft.AspNetCore.Mvc;

namespace DataDock.Web.Controllers
{
    public class LoaderController : Controller
    {
        public IActionResult Jobs (string ownerId, string repoId)
        {
            return ViewComponent("JobHistory", new { selectedOwnerId = ownerId, selectedRepoId = repoId });
        }

        public IActionResult Datasets(string ownerId, string repoId)
        {
            return ViewComponent("Datasets", new { selectedOwnerId = ownerId, selectedRepoId = repoId });
        }
    }
}