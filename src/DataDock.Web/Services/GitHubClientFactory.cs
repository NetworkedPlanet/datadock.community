using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Datadock.Common.Models;
using Octokit;
using Serilog;

namespace DataDock.Web.Services
{
    public class GitHubClientFactory : IGitHubClientFactory
    {
        private readonly string _appName;
        public GitHubClientFactory(string appName)
        {
            _appName = appName;
        }

        public IGitHubClient CreateClient(ClaimsIdentity identity)
        {
            try
            {
                Log.Debug("CreateClient for app '{0}'", _appName);

                if (identity == null)
                {
                    Log.Error("Unable to initialise GitHubClient, no user identity supplied.");
                    return null;
                }
                var claims = identity.Claims;
                var accessTokenClaim = claims.FirstOrDefault(x => x.Type == DataDockClaimTypes.GitHubAccessToken);
                var accessToken = accessTokenClaim?.Value;
                if (string.IsNullOrEmpty(accessToken))
                {
                    Log.Error("Unable to initialise GitHubClient, no access token found.");
                    return null;
                }
                //set credentials to current logged-in user
                var creds = new Credentials(accessToken);

                return new GitHubClient(new ProductHeaderValue(_appName))
                {
                    Credentials = creds
                };
            }
            catch (Exception e)
            {
                Log.Error(e, "Error creating GitHubClient");
                throw;
            }

        }
    }
}
