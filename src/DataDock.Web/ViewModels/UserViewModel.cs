using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Datadock.Common.Models;
using DataDock.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
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

        public List<Owner> Organisations { get; set; }

        public UserViewModel()
        {
            this.Organisations = new List<Owner>();
        }

        public void Populate(ClaimsIdentity identity)
        {
            if (identity.IsAuthenticated)
            {
                GitHubName = identity.FindFirst(c => c.Type == ClaimTypes.Name)?.Value;
                GitHubLogin = identity.FindFirst(c => c.Type == "urn:github:login")?.Value;
                GitHubUrl = identity.FindFirst(c => c.Type == "urn:github:url")?.Value;
                GitHubAvatar = identity.FindFirst(c => c.Type == "urn:github:avatar")?.Value;

                foreach (var orgClaim in identity.Claims.Where(c =>
                    c.Type.Equals((DataDockClaimTypes.GitHubUserOrganization))))
                {
                    Organisations.Add(JsonConvert.DeserializeObject<Owner>(orgClaim.Value));
                }
                
            }
        }
    }
}
