using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Octokit;
using Octokit.Internal;

namespace DataDock.Web.ViewModels
{
    public class UserViewModel
    {
        public string GitHubAvatar { get; set; }

        public string GitHubLogin { get; set; }

        public string GitHubName { get; set; }

        public string GitHubUrl { get; set; }

        public IReadOnlyList<Repository> Repositories { get; set; }

        public async Task Populate(ClaimsIdentity identity)
        {
            if (identity.IsAuthenticated)
            {
                GitHubName = identity.FindFirst(c => c.Type == ClaimTypes.Name)?.Value;
                GitHubLogin = identity.FindFirst(c => c.Type == "urn:github:login")?.Value;
                GitHubUrl = identity.FindFirst(c => c.Type == "urn:github:url")?.Value;
                GitHubAvatar = identity.FindFirst(c => c.Type == "urn:github:avatar")?.Value;

                //string accessToken = await HttpContext.GetTokenAsync("access_token");

                //var github = new GitHubClient(new ProductHeaderValue("AspNetCoreGitHubAuth"),
                //    new InMemoryCredentialStore(new Credentials(accessToken)));
                //Repositories = await github.Repository.GetAllForCurrent();
            }
        }
    }
}
