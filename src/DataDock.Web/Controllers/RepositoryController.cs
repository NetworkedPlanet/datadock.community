using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace DataDock.Web.Controllers
{
    public class RepositoryController : DashboardBaseController
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}