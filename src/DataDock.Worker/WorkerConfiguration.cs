using DataDock.Common;
using Serilog;

namespace DataDock.Worker
{
    public class WorkerConfiguration : ApplicationConfiguration
    {
        /// <summary>
        /// The root URL for published datasets
        /// </summary>
        public string PublishUrl { get; set; } = "http://datadock.io/";

        /// <summary>
        /// The path to the Git executable
        /// </summary>
        public string GitPath { get; set; } = "git";

        /// <summary>
        /// The path to the directory to use for cloning user repositories
        /// </summary>
        public string RepoBaseDir { get; set; } = "/datadock/repositories";

        /// <summary>
        /// The header value that identifies the DataDock GitHub integration to be inserted into Octokit requests
        /// </summary>
        public string GitHubProductHeader { get; set; } = "";

        public override void LogSettings()
        {
            base.LogSettings();
            Log.Information("Configured Publish URL {PublishUrl}", PublishUrl);
            Log.Information("Configured Git Path {GitPath}", GitPath);
            Log.Information("Configured Repository Base Directory {RepoBaseDir}", RepoBaseDir);
            Log.Information("Configured GitHub Product Header {GitHubProductHeader}", GitHubProductHeader);
        }
    }
}