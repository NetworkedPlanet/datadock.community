using System.Collections.Generic;
using System.Threading.Tasks;
using Datadock.Common.Models;

namespace Datadock.Common.Stores
{
    public interface IRepoSettingsStore
    {
        Task<IEnumerable<RepoSettings>> GetRepoSettingsForOwnerAsync(string ownerId);
        Task<RepoSettings> GetRepoSettingsAsync(string ownerId, string repositoryId);
        Task CreateOrUpdateRepoSettingsAsync(RepoSettings settings);
        Task<bool> DeleteRepoSettingsAsync(string ownerId, string repositoryId);
        Task<RepoSettings> GetRepoSettingsByIdAsync(string id);
    }
}
