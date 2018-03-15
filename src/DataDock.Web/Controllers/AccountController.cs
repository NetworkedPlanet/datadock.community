using System;
using System.Linq;
using System.Threading.Tasks;
using DataDock.Web.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataDock.Web.Controllers
{
    [Route("[controller]/[action]")]
    public class AccountController : Controller
    {
        [HttpGet]
        public async Task<IActionResult> SignUp()
        {
            return View("SignUp");
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = "/")
        {
            return Challenge(new AuthenticationProperties() { RedirectUri = returnUrl });
        }

        [HttpGet]
        public async Task<IActionResult> LogOff(string returnUrl = "/")
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
        
        [HttpGet]
        [Authorize(Policy = "DataDockNewUser")]
        [ServiceFilter(typeof(AuthorizeFilter))]
        public IActionResult Settings(string returnUrl = "/")
        {
            var claims = User.Claims.ToList();
            return View("Settings");
        }
    }
}