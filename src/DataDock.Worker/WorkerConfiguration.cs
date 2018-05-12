using DataDock.Common;
using Serilog;

namespace DataDock.Worker
{
    public class WorkerConfiguration : ApplicationConfiguration
    {
        public WorkerConfiguration(string esUrl, string jobsIndex, string userIndex, string ownerSettingsIndex,
            string repoSettingsIndex, string datasetIndex, string schemaIndex, string fileStorePath,
            string gitPath, string repoBaseDir, string gitHubProductHeader) :
            base(esUrl, jobsIndex, userIndex, ownerSettingsIndex, repoSettingsIndex, datasetIndex, schemaIndex, fileStorePath)
        {
            GitPath = gitPath;
            RepoBaseDir = repoBaseDir;
            GitHubProductHeader = gitHubProductHeader;
        }

        /// <summary>
        /// The path to the Git executable
        /// </summary>
        public string GitPath { get; set; }

        /// <summary>
        /// The path to the directory to use for cloning user repositories
        /// </summary>
        public string RepoBaseDir { get; set; }

        /// <summary>
        /// The header value that identifies the DataDock GitHub integration to be inserted into Octokit requests
        /// </summary>
        public string GitHubProductHeader { get; set; }

        public new static WorkerConfiguration FromEnvironment()
        {
            Log.Information("Retreiving application configuration from environment variables");
            return new WorkerConfiguration(
                GetEnvVar("ES_URL", "http://elasticsearch:9200"),
                GetEnvVar("JOBS_IX", "jobs"),
                GetEnvVar("USER_IX", "users"),
                GetEnvVar("OWNERSETTINGS_IX", "ownersettings"),
                GetEnvVar("REPOSETTINGS_IX", "reposettings"),
                GetEnvVar("DATASET_IX", "datasets"),
                GetEnvVar("SCHEMA_IX", "schemas"),
                GetEnvVar("FILE_STORE_PATH", "/datadock/files"),
                GetEnvVar("GIT_PATH", "git"),
                GetEnvVar("REPO_BASE_DIR", "/datadock/repositories"),
                GetEnvVar("GITHUB_HEADER", ""));
        }

        public override void LogSettings()
        {
            base.LogSettings();
            Log.Information("Configured Git Path {GitPath}", GitPath);
            Log.Information("Configured Repository Base Directory {RepoBaseDir}", RepoBaseDir);
        }
    }
}