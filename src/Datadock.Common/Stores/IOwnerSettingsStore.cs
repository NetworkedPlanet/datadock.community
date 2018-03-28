using System.Threading.Tasks;
using Datadock.Common.Models;

namespace Datadock.Common.Stores
{
    public interface IOwnerSettingsStore
    {
        Task<OwnerSettings> GetOwnerSettingsAsync(string ownerId);

        Task CreateOrUpdateOwnerSettingsAsync(OwnerSettings ownerSettings);
    }
}
