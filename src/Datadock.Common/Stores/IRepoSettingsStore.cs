using System.Threading.Tasks;
using Datadock.Common.Models;

namespace Datadock.Common.Stores
{
    public interface IRepoSettingsStore
    {
        Task<RepoSettings> GetRepoSettingsAsync(string ownerId, string repoId);
        Task CreateOrUpdateRepoSettingsAsync(RepoSettings settings);
    }
}
