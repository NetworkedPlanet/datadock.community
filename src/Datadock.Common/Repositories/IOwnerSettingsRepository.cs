using System.Threading.Tasks;
using Datadock.Common.Models;

namespace Datadock.Common.Repositories
{
    public interface IOwnerSettingsRepository
    {
        Task<OwnerSettings> GetOwnerSettingsAsync(string ownerId);

        Task CreateOrUpdateOwnerSettingsAsync(OwnerSettings ownerSettings);
    }
}
