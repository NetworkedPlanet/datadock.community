using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace DataDock.Web.Controllers
{
    [Route("[controller]/[action]")]
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login(string returnUrl = "/")
        {
            return Challenge(new AuthenticationProperties() { RedirectUri = returnUrl });
        }

        [HttpGet]
        public IActionResult LogOff(string returnUrl = "/")
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        public IActionResult SignUp(string returnUrl = "/")
        {
            throw new NotImplementedException();
        }
    }
}