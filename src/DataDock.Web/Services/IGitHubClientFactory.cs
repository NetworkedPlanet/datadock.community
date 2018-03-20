using Octokit;
using System.Security.Claims;

namespace DataDock.Web.Services
{
    public interface IGitHubClientFactory
    {
        IGitHubClient CreateClient(ClaimsIdentity identity);
    }
}
