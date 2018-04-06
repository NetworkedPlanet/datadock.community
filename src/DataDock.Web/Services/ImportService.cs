﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Datadock.Common.Models;
using Datadock.Common.Stores;
using Octokit;
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
            var repo = await CheckGitHubRepository(user.Identity, ownerId, repoId);
            try
            {
                var repoSettings = await _repoSettingsStore.GetRepoSettingsAsync(ownerId, repoId);
                if (string.IsNullOrEmpty(repoSettings.CloneUrl))
                {
                    repoSettings.CloneUrl = repo.CloneUrl;
                    await _repoSettingsStore.CreateOrUpdateRepoSettingsAsync(repoSettings);
                }
                return repoSettings;
            }
            catch (RepoSettingsNotFoundException rsnf)
            {
                var newRepoSettings = new RepoSettings
                {
                    OwnerId = ownerId,
                    RepoId = repoId,
                    Name = repo.Name,
                    CloneUrl = repo.CloneUrl,
                    OwnerAvatar = repo.Owner?.AvatarUrl
                };

                await _repoSettingsStore.CreateOrUpdateRepoSettingsAsync(newRepoSettings);
                return newRepoSettings;
            }
            catch (Exception e)
            {
                Log.Error($"Error checking repo settings for '{ownerId}/{repoId}", e);
                throw;
            }
        }

        private async Task<Repository> CheckGitHubRepository(IIdentity identity, string ownerId, string repoId)
        {
            if (identity == null) throw new ArgumentNullException();
            if (string.IsNullOrEmpty(ownerId)) throw new ArgumentException("ownerId parameter is null or empty");

            // check user has access to the github owner account
            try
            {
                if (!identity.Name.Equals(ownerId))
                {
                    var userHasOwner = await _gitHubApiService.UserIsAuthorizedForOrganization(identity, ownerId);
                    if (!userHasOwner) throw new UnauthorizedAccessException();
                }
               
                // does github repo exist
                var repo = await _gitHubApiService.GetRepositoryAsync(identity, ownerId, repoId);
                if (repo == null) throw new Exception($"No public repository '{repoId} found for owner '{ownerId}'");
                return repo;
            }
            catch (Exception e)
            {
                Log.Error($"Error checking github repository existence for '{ownerId}/{repoId}", e);
                throw;
            }
        }
    }
}
