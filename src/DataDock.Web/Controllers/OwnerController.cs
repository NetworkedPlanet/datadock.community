using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DataDock.Web.Controllers
{
    [Authorize(Policy = "User")]
    public class OwnerController : Controller
    {
        public async Task<IActionResult> Index()
        {
            var user = User.Identity;

            var userViewModel = new UserViewModel();
            await userViewModel.Populate(user as ClaimsIdentity);

            return View(userViewModel);
        }
    }
}