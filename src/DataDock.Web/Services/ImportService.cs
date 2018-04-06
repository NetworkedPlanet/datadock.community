using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Datadock.Common.Models;
using Datadock.Common.Stores;
using Serilog;

namespace DataDock.Web.Services
{
    public class ImportService : IImportService
    {
        private readonly IGitHubApiService _gitHubApiService;
        private readonly IRepoSettingsStore _repoSettingsStore;
        public ImportService(IGitHubApiService gitHubApiService,
            IRepoSettingsStore repoSettingsStore)
        {
            _gitHubApiService = gitHubApiService;
            _repoSettingsStore = repoSettingsStore;
        }

        /// <summary>
        /// Check that the user has access to the org and repo then
        /// return the settings for that repository, create settings if none exists
        /// </summary>
        /// <param name="user"></param>
        /// <param name="ownerId"></param>
        /// <param name="repoId"></param>
        /// <returns></returns>
        public async Task<RepoSettings> CheckRepoSettingsAsync(ClaimsPrincipal user, string ownerId, string repoId)
        {
            try
            {
                var repoSettings = await _repoSettingsStore.GetRepoSettingsAsync(ownerId, repoId);
                return repoSettings;
            }
            catch (RepoSettingsNotFoundException rsnf)
            {
                var newRepoSettings = new RepoSettings
                {
                    OwnerId = ownerId,
                    RepositoryId = repoId
                };
                await _repoSettingsStore.CreateOrUpdateRepoSettingsAsync(newRepoSettings);
                return newRepoSettings;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private async Task CheckOwnerAndRepo(IIdentity identity, string ownerId)
        {
            if (identity == null) throw new ArgumentNullException();
            if (string.IsNullOrEmpty(ownerId)) throw new ArgumentException("ownerId parameter is null or empty");

            // check user has access to the github owner account
            try
            {
                var userHasOwner = await _gitHubApiService.UserIsAuthorizedForOrganization(identity, ownerId);
                if(!userHasOwner) throw new UnauthorizedAccessException();

                // does repo exist on github

                
            }
            catch (Exception e)
            {
                Log.Error("Unable to check user has access to the owner", e);
                throw;
            }
        }
    }
}
