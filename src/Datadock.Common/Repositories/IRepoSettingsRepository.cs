using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Datadock.Common.Models;

namespace Datadock.Common.Repositories
{
    public interface IRepoSettingsRepository
    {
        Task<RepoSettings> GetRepoSettingsAsync(string ownerId, string repoId);
        Task CreateOrUpdateRepoSettingsAsync(RepoSettings settings);
    }
}
