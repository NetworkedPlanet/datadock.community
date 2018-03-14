using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DataDock.Web.Controllers
{
    public class HomeController : Controller
    {
        public async Task<IActionResult> Index()
        {
            var user = User.Identity;
            
            var userViewModel = new UserViewModel();
            await userViewModel.Populate(user as ClaimsIdentity);

            return View(userViewModel);
        }


        public IActionResult Error()
        {
            ViewData["RequestId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            return View();
        }
    }
}
