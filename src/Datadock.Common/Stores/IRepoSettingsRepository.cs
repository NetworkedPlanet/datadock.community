using System.Threading.Tasks;
using Datadock.Common.Models;

namespace Datadock.Common.Stores
{
    public interface IRepoSettingsRepository
    {
        Task<RepoSettings> GetRepoSettingsAsync(string ownerRepoId);
        Task CreateOrUpdateRepoSettingsAsync(RepoSettings settings);
    }
}
