using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Octokit;
using Serilog;

namespace DataDock.Web.Services
{
    public class GitHubApiService : IGitHubApiService
    {
        private readonly IGitHubClientFactory _gitHubClientFactory;

        public GitHubApiService(IGitHubClientFactory gitHubClientFactory)
        {
            _gitHubClientFactory = gitHubClientFactory;
        }

        public async Task<List<string>> GetOwnerIdsForUserAsync(string userName, ClaimsIdentity identity)
        {
            if (string.IsNullOrEmpty(userName)) throw new ArgumentException("userName parameter is null or empty");
            if (identity == null) throw new ArgumentNullException();
            var ownerIdList = new List<string> { userName };
            try
            {
                var ghClient = _gitHubClientFactory.CreateClient(identity);
                var orgs = await ghClient.Organization.GetAllForCurrent();
                if (orgs == null || !orgs.Any())
                {
                    Log.Warning("GetOwnerIdsForuserAsync: No organizations returned for user '{0}'", userName);
                    return ownerIdList;
                }
                Log.Debug("GetOwnerIdsForuserAsync: {0} organizations found for user '{1}'", orgs.Count(), userName);
                ownerIdList.AddRange(orgs.Select(o => o.Login));
                return ownerIdList;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetOwnerIdsForuserAsync: Error retrieving organization list for user {0}.", userName);
                Log.Error(ex.ToString());
                return null;
            }
        }
        
        public async Task<List<Organization>> GetOrganizationsForUserAsync(string userName, ClaimsIdentity identity)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> UserIsAuthorizedForOrganization(string userName, ClaimsIdentity identity, string ownerId)
        {
            throw new NotImplementedException();
        }

        public async Task<List<Repository>> GetRepositoriesForUserAsync(string userName, ClaimsIdentity identity)
        {
            throw new NotImplementedException();
        }

        public async Task<List<Repository>> GetRepositoriesForOwnerAsync(ClaimsIdentity identity, string ownerId, bool isOrg = false)
        {
            throw new NotImplementedException();
        }

        public async Task<Repository> GetRepositoryAsync(ClaimsIdentity identity, string ownerId, string repoShortId)
        {
            throw new NotImplementedException();
        }
    }
}
