using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Datadock.Common.Repositories;
using DataDock.Web.Auth;
using DataDock.Web.Models;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
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

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Cancel()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("SignUpCancelled", "Info");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SignUp(SignUpViewModel signUpViewModel)
        {
            if (ModelState.IsValid && signUpViewModel.AgreeTerms)
            {
                try
                {
                    // create user
                    var newUser = await _userRepository.CreateUserAsync(User.Identity.Name, User.Claims);
                    if (newUser == null)
                    {
                        Log.Error("Creation of new user account returned null");
                        return RedirectToAction("Error", "Home");
                    }
                    // add new claim
                    var datadockIdentity = new ClaimsIdentity();
                    datadockIdentity.AddClaim(new Claim(DataDockClaimTypes.DataDockUserId, User.Identity.Name));
                    User.AddIdentity(datadockIdentity);

                    return RedirectToAction("Welcome");
                }
                catch (UserAccountExistsException existsEx)
                {
                    Log.Warning("User account already exists");
                    var existingUser = await _userRepository.GetUserAccountAsync(User.Identity.Name);
                    if (existingUser.Claims.FirstOrDefault(c => c.Type.Equals(DataDockClaimTypes.DataDockUserId)) ==
                        null)
                    {
                        // add new claim
                        var datadockIdentity = new ClaimsIdentity();
                        datadockIdentity.AddClaim(new Claim(DataDockClaimTypes.DataDockUserId, User.Identity.Name));
                        User.AddIdentity(datadockIdentity);

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
        [ServiceFilter(typeof(AccountExistsFilter))]
        public async Task<IActionResult> Settings(string returnUrl = "/")
        {
            try
            {
                var userSettings = await _userRepository.GetUserSettingsAsync(User.Identity.Name);
                var usvm = new UserSettingsViewModel(userSettings);
                return View(usvm);
            }
            catch (UserAccountNotFoundException notFoundEx)
            {
                var newSettings = new UserSettingsViewModel {UserId = User.Identity.Name};
                return View(newSettings);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [HttpPost]
        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        public async Task<IActionResult> Settings(UserSettingsViewModel usvm)
        {
            try
            {
                usvm.LastModified = DateTime.UtcNow;
                usvm.LastModifiedBy = User.Identity.Name;
                var userSettings = usvm.AsUserSettings();
                await _userRepository.CreateOrUpdateUserSettingsAsync(userSettings);

                ViewBag.StatusMessage = GetSettingsStatusMessage(ManageMessageId.ChangeSettingSuccess);
                
                return View(usvm);
            }
            catch (Exception e)
            {
                Log.Error(e, "An error occurred loading user settings for '{0}'", User.Identity.Name);
                ViewBag.StatusMessage = GetSettingsStatusMessage(ManageMessageId.Error);
                return View(new UserSettingsViewModel { UserId = User.Identity.Name });
            }
        }

        public string GetSettingsStatusMessage(ManageMessageId? message = null)
        {
            if (message == null)
            {
                // check in TempData if a message isn't directly supplied
                message = TempData["message"] as ManageMessageId?;
            }
            var statusMessage = message == ManageMessageId.ChangeSettingSuccess
                ? @"The settings have been successfully updated."
                : message == ManageMessageId.Error
                    ? @"An error has occurred."
                    : message == ManageMessageId.TokenResetError ?
                        @"Unable to reset token." :
                        @"";
            return statusMessage;
        }

        [HttpGet]
        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        public async Task<IActionResult> Delete(string returnUrl = "")
        {
            try
            {
                // check user exists
                var userAccount = await _userRepository.GetUserAccountAsync(User.Identity.Name);
                var davm = new DeleteAccountViewModel();
                return View(davm);
            }
            catch (Exception e)
            {
                // todo handle user not found
                Console.WriteLine(e);
                throw;
            }
        }

        [HttpPost]
        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        public async Task<IActionResult> Delete(DeleteAccountViewModel davm)
        {
            try
            {
                if (ModelState.IsValid && davm.Confirm)
                {
                    var deleted = await _userRepository.DeleteUserAsync(User.Identity.Name);
                    if (deleted)
                    {
                        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        return RedirectToAction("Index", "Home");
                    }

                    // error
                    ViewBag.Message =
                        "Unable to delete account at this time, if the problem persists please open a ticket with support.";
                    return View("Delete");
                }

                return View("Delete");

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [HttpGet]
        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        public IActionResult Welcome(string returnUrl = "/")
        {
            return View("Welcome");
        }
    }
}