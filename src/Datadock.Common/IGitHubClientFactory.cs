using Octokit;

namespace Datadock.Common
{
    public interface IGitHubClientFactory
    {
        GitHubClient GetClient(string accessToken);
    }

}
