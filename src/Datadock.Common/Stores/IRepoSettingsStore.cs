using System.Threading.Tasks;
using Datadock.Common.Models;

namespace Datadock.Common.Stores
{
    public interface IRepoSettingsStore
    {
        Task<RepoSettings> GetRepoSettingsAsync(string ownerRepoId);
        Task CreateOrUpdateRepoSettingsAsync(RepoSettings settings);
    }
}
