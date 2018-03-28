﻿using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Datadock.Common.Models;
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
            Log.Debug("User: {0}. Identities: {1}. Claims Total: {2}", User.Identity.Name, User.Identities.Count(), User.Claims.Count());

            if (User.ClaimExists(DataDockClaimTypes.DataDockUserId))
            {
                return RedirectToAction("Settings");
            }

            try
            {
                // check for user in datadock just in case of login  / claims problems
                var datadockUser = await _userRepository.GetUserAccountAsync(User.Identity.Name);
                if (datadockUser != null)
                {
                    // add identity to context User
                    // new datadock identity inc claim
                    var datadockIdentity = new ClaimsIdentity();
                    datadockIdentity.AddClaim(new Claim(DataDockClaimTypes.DataDockUserId, User.Identity.Name));
                    User.AddIdentity(datadockIdentity);
                    if (datadockUser.Claims.FirstOrDefault(c => c.Type.Equals(DataDockClaimTypes.DataDockUserId)) ==
                        null)
                    {
                        // update datadock user account if required
                        await _userRepository.UpdateUserAsync(User.Identity.Name, User.Claims);

                    }

                    // logout and back in to persist new claims
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return Challenge(new AuthenticationProperties() {RedirectUri = "account/settings"});
                }

            }
            catch (UserAccountNotFoundException noUserException)
            {
                var viewModel = new SignUpViewModel
                {
                    Title = "DataDock New User",
                    Heading = "Sign Up to DataDock"
                };
                return View("SignUp", viewModel);
            }
            catch (Exception ex)
            {
                Log.Error("Error creating user account", ex);
                Console.WriteLine(ex);
                throw;
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
                    // new datadock identity inc claim
                    var datadockIdentity = new ClaimsIdentity();
                    datadockIdentity.AddClaim(new Claim(DataDockClaimTypes.DataDockUserId, User.Identity.Name));
                    User.AddIdentity(datadockIdentity);

                    // create user in datadock
                    var newUser = await _userRepository.CreateUserAsync(User.Identity.Name, User.Claims);
                    if (newUser == null)
                    {
                        Log.Error("Creation of new user account returned null");
                        return RedirectToAction("Error", "Home");
                    }

                    // logout and back in to persist new claims
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return Challenge(new AuthenticationProperties() { RedirectUri = "account/welcome" });
                }
                catch (UserAccountExistsException existsEx)
                {
                    Log.Warning("User account {0} already exists. Identities: {1}. Claims Total: {2}", User.Identity.Name, User.Identities.Count(), User.Claims.Count());
                    var datadockUser = await _userRepository.GetUserAccountAsync(User.Identity.Name);
                    if (datadockUser.Claims.FirstOrDefault(c => c.Type.Equals(DataDockClaimTypes.DataDockUserId)) ==
                        null)
                    {
                        // new datadock identity inc claim
                        var datadockIdentity = new ClaimsIdentity();
                        datadockIdentity.AddClaim(new Claim(DataDockClaimTypes.DataDockUserId, User.Identity.Name));
                        User.AddIdentity(datadockIdentity);
                        await _userRepository.UpdateUserAsync(User.Identity.Name, User.Claims);
                        // logout and back in to persist new claims
                        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        return Challenge(new AuthenticationProperties() { RedirectUri = "account/settings" });
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
            return View("Welcome", new BaseLayoutViewModel{Title = "Welcome to DataDock"});
        }
    }
}