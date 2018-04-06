using System.Collections.Generic;
using System.Threading.Tasks;
using Datadock.Common.Models;

namespace Datadock.Common.Stores
{
    public interface IRepoSettingsStore
    {
        Task<IEnumerable<RepoSettings>> GetRepoSettingsForOwnerAsync(string ownerId);
        Task<RepoSettings> GetRepoSettingsAsync(string ownerId, string repoId);
        Task CreateOrUpdateRepoSettingsAsync(RepoSettings settings);
    }
}
