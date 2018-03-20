using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using DataDock.Web.Auth;

namespace DataDock.Web.Controllers
{
    [Authorize]
    [ServiceFilter(typeof(AccountExistsFilter))]
    public class OwnerController : Controller
    {
        public async Task<IActionResult> Index()
        {
            var user = User.Identity;

            var userViewModel = new UserViewModel();
            userViewModel.Populate(user as ClaimsIdentity);

            return View(userViewModel);
        }
    }
}