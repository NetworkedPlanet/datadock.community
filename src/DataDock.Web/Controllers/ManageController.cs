using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataDock.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataDock.Web.Controllers
{
    [Authorize(Policy = "Admin")]
    [ServiceFilter(typeof(AccountExistsFilter))]
    public class ManageController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}