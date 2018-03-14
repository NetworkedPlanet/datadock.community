using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace DataDock.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        public async Task<IActionResult> Index()
        {
            var userViewModel = new UserViewModel();
            if (User.Identity.IsAuthenticated)
            {
                var user = User.Identity;
                await userViewModel.Populate(user as ClaimsIdentity);
            }

            return View(userViewModel);

        }
        
    }
}
