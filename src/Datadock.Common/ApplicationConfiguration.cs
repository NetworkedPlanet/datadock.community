using System;
using Serilog;

namespace DataDock.Common
{
    public class ApplicationConfiguration 
    {
        public string ElasticsearchUrl { get; }
        public string JobsIndexName { get; }
        public string UserIndexName { get; }
        public string OwnerSettingsIndexName { get; }
        public string RepoSettingsIndexName { get; }
        public string DatasetIndexName { get; }
        public string SchemaIndexName { get; }
        public string FileStorePath { get; }

        public ApplicationConfiguration(string esUrl, string jobsIndex, string userIndex, string ownerSettingsIndex,
            string repoSettingsIndex, string datasetIndex, string schemaIndex, string fileStorePath)
        {
            ElasticsearchUrl = esUrl;
            JobsIndexName = jobsIndex;
            UserIndexName = userIndex;
            OwnerSettingsIndexName = ownerSettingsIndex;
            RepoSettingsIndexName = repoSettingsIndex;
            DatasetIndexName = datasetIndex;
            SchemaIndexName = schemaIndex;
            FileStorePath = fileStorePath;
        }

        public virtual void LogSettings()
        {
            Log.Information("Configured Elasticsearch Url {ElasticsearchUrl}", ElasticsearchUrl);
            Log.Information("Configured Jobs Index {JobsIndexName}", JobsIndexName);
            Log.Information("Configured User Index {UserIndexName}", UserIndexName);
            Log.Information("Configured Owner Settings Index {OwnerSettingsIndexName}", OwnerSettingsIndexName);
            Log.Information("Configured Repository Settings Index {RepoSettingsIndexName}", RepoSettingsIndexName);
            Log.Information("Configured DatasetIndex {DatasetIndexName}", DatasetIndexName);
            Log.Information("Configured Schema Index {SchemaIndexName}", SchemaIndexName);
            Log.Information("Configured File Store Path {FileStorPath}", FileStorePath);
        }

        public static ApplicationConfiguration FromEnvironment()
        {
            Log.Information("Retreiving application configuration from environment variables");
            return new ApplicationConfiguration(
                GetEnvVar("ES_URL", "http://elasticsearch:9200"),
                GetEnvVar("JOBS_IX", "jobs"),
                GetEnvVar("USER_IX", "users"),
                GetEnvVar("OWNERSETTINGS_IX", "ownersettings"),
                GetEnvVar("REPOSETTINGS_IX", "reposettings"),
                GetEnvVar("DATASET_IX", "datasets"),
                GetEnvVar("SCHEMA_IX", "schemas"),
                GetEnvVar("FILE_STORE_PATH", "/datadock/files"));
        }

        protected static string GetEnvVar(string var, string defaultValue)
        {
            return Environment.GetEnvironmentVariable(var) ?? defaultValue;
        }
    }
}