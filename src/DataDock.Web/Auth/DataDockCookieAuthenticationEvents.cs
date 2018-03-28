using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Datadock.Common.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace DataDock.Web.Auth
{
    public class DataDockCookieAuthenticationEvents: CookieAuthenticationEvents
    {
        private readonly IUserRepository _userRepository;

        public DataDockCookieAuthenticationEvents(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
        {
            var userPrincipal = context.Principal;


            var userId = (from c in userPrincipal.Claims
                where c.Type == "UserId"
                select c.Value).FirstOrDefault();
            if (!string.IsNullOrEmpty(userId))
            {
                // Look for the LastChanged claim.
                var lastChanged = (from c in userPrincipal.Claims
                    where c.Type == "LastChanged"
                    select c.Value).FirstOrDefault();

                DateTime.TryParse(lastChanged, CultureInfo.InvariantCulture, DateTimeStyles.None,
                    out var lastChangedDateTime);
                
                if (string.IsNullOrEmpty(lastChanged))
                {
                    var validate = await _userRepository.ValidateLastChanged(userId, lastChangedDateTime);
                    if (!validate)
                    {
                        context.RejectPrincipal();

                        await context.HttpContext.SignOutAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme);
                    }

                }
            }
        }
    }
}
