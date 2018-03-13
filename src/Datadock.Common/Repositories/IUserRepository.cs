using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Claims;
using System.Threading.Tasks;
using Datadock.Common.Models;

namespace Datadock.Common.Repositories
{
    public interface IUserRepository
    {
        /// <summary>
        /// Retrieve the settings for the specified user
        /// </summary>
        /// <param name="userId">The datadock user id</param>
        /// <returns>The settings for the user or null if no settings could be found</returns>
        Task<UserSettings> GetUserSettingsAsync(string userId);

        /// <summary>
        /// Add or update user settings
        /// </summary>
        /// <param name="userSettings">The settings to be updated / created</param>
        Task CreateOrUpdateUserSettingsAsync(UserSettings userSettings);

        /// <summary>
        /// Create a new user account
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<UserAccount> CreateUserAsync(string userId, IEnumerable<Claim> accountClaims);

        /// <summary>
        /// Remove the settings for the specified user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>True if successfully deleted.</returns>
        Task<bool> DeleteUserAsync(string userId);

        Task<UserAccount> GetUserAccountAsync(string userId);

        Task <bool> ValidateLastChanged(string userId, DateTime lastChanged);
    }
}
