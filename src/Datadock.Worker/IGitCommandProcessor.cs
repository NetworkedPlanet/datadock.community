using System.Threading.Tasks;
using Datadock.Common.Models;
using VDS.RDF;

namespace DataDock.Worker
{
    public interface IGitCommandProcessor
    {
        Task CloneRepository(string repository, string targetDirectory, string authenticationToken, UserAccount userAccount);
        Task<bool> CommitChanges(string repositoryDirectory, string commitMessage, UserAccount userAccount);
        Task<ReleaseInfo> MakeRelease(IGraph dataGraph, string releaseTag, string owner, string repositoryId, string datasetId, string repositoryDirectory, string authenticationToken);
        Task PushChanges(string remoteUrl, string repositoryDirectory, string authenticationToken, bool setUpstream = false, string branch = "gh-pages");
    }
}