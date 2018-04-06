using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Datadock.Common.Models;


namespace DataDock.Web.Services
{
    public interface IImportService
    {
        Task<RepoSettings> CheckRepoSettingsAsync(ClaimsPrincipal user, string ownerId, string repoId);
    }
}
