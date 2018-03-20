using Octokit;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DataDock.Web.Services
{
    public interface IGitHubApiService
    {
        Task<List<string>> GetOwnerIdsForUserAsync(string userName, ClaimsIdentity identity);

        Task<List<Organization>> GetOrganizationsForUserAsync(string userName, ClaimsIdentity identity);

        Task<bool> UserIsAuthorizedForOrganization(string userName, ClaimsIdentity identity, string ownerId);

        Task<List<Repository>> GetRepositoriesForUserAsync(string userName, ClaimsIdentity identity);

        Task<List<Repository>> GetRepositoriesForOwnerAsync(ClaimsIdentity identity, string ownerId, bool isOrg = false);

        //Task<OwnerViewModel> GetUserInfoAsync(string userName, ClaimsIdentity identity);

        //Task<List<OwnerViewModel>> GetUserOrgInfoListAsync(string userName, ClaimsIdentity identity);
        
        Task<Repository> GetRepositoryAsync(ClaimsIdentity identity, string ownerId, string repoShortId);
    }
}
