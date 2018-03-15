﻿using Microsoft.AspNetCore.Mvc;

namespace DataDock.Web.Controllers
{
    public class InfoController : Controller
    {

        public IActionResult About()
        {
            return View("About");
        }

        public IActionResult Features()
        {
            return View("Features");
        }

        public IActionResult Help()
        {
            return View("Help");
        }

        public IActionResult Privacy()
        {
            return View("Privacy");
        }

        public IActionResult Terms()
        {
            return View("Terms");
        }

        public IActionResult AccountDeleted()
        {
            return View("AccountDeleted");
        }

        public IActionResult SignUpCancelled()
        {
            return View("SignUpCancelled");
        }
    }
}