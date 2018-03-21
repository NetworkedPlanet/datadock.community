using Datadock.Common.Models;
using System.Threading.Tasks;

namespace Datadock.Common.Repositories
{
    public interface IRepoSettingsRepository
    {
        Task<RepoSettings> GetRepoSettingsAsync(string ownerRepoId);
        Task CreateOrUpdateRepoSettingsAsync(RepoSettings settings);
    }
}
