using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Datadock.Common.Repositories;
using DataDock.Web.Auth;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace DataDock.Web.Controllers
{
    [Route("[controller]/[action]")]
    public class AccountController : Controller
    {
        private readonly IUserRepository _userRepository;
        public AccountController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = "/")
        {
            return Challenge(new AuthenticationProperties() { RedirectUri = returnUrl });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> LogOff(string returnUrl = "/")
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Forbidden(string returnUrl = "/")
        {
            Log.Warning("User '{0}' has attempted to access forbidden page {1}", User?.Identity?.Name, returnUrl);
            return View("Forbidden");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> SignUp()
        {
            if (User.ClaimExists(DataDockClaimTypes.DataDockUserId))
            {
                return RedirectToAction("Settings");
            }
            return View("SignUp");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SignUp(SignUpViewModel signUpViewModel)
        {
            if (ModelState.IsValid && signUpViewModel.AgreeTerms)
            {
                try
                {
                    // add new claim
                    var identity = User.Identity as ClaimsIdentity;
                    identity?.AddClaim(new Claim(DataDockClaimTypes.DataDockUserId, User.Identity.Name));
                    // create user
                    var newUser = await _userRepository.CreateUserAsync(User.Identity.Name, User.Claims);
                    if (newUser == null)
                    {
                        Log.Error("Creation of new user account returned null");
                        return RedirectToAction("Error", "Home");
                    }
                    return RedirectToAction("Welcome");
                }
                catch (UserAccountExistsException existsEx)
                {
                    Log.Warning("User account already exists");
                    var existingUser = await _userRepository.GetUserAccountAsync(User.Identity.Name);
                    if (existingUser.Claims.FirstOrDefault(c => c.Type.Equals(DataDockClaimTypes.DataDockUserId)) ==
                        null)
                    {
                        // add claim
                        Log.Warning("Adding DataDock user Id to claims account");
                        var identity = User.Identity as ClaimsIdentity;
                        identity?.AddClaim(new Claim(DataDockClaimTypes.DataDockUserId, User.Identity.Name));
                        try
                        {
                            var updatedUser = await _userRepository.UpdateUserAsync(User.Identity.Name, User.Claims);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                    }
                    return RedirectToAction("Settings");
                }
                catch (Exception ex)
                {
                    Log.Error("Error creating user account", ex);
                    Console.WriteLine(ex);
                    throw;
                }
            }

            ViewBag.Success = "failed";
            return View(signUpViewModel);
        }
 
        [HttpGet]
        [Authorize]
        [Authorize(Policy = "User")]
        public IActionResult Settings(string returnUrl = "/")
        {
            var claims = User.Claims.ToList();
            return View("Settings");
        }

        [HttpGet]
        [Authorize(Policy = "User")]
        public IActionResult Welcome(string returnUrl = "/")
        {
            var claims = User.Claims.ToList();
            return View("Welcome");
        }
    }
}