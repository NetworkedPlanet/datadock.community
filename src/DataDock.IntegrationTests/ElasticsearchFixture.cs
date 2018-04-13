using System;
using System.IO;
using DataDock.Common;
using DataDock.Worker;
using Nest;

namespace DataDock.IntegrationTests
{
    public class ElasticsearchFixture : IDisposable
    {
        public ApplicationConfiguration Configuration { get; }
        public WorkerConfiguration WorkerConfiguration { get; }

        public ElasticClient Client { get; }

        public ElasticsearchFixture()
        {
            var esUrl = Environment.GetEnvironmentVariable("ELASTICSEARCH_URL") ?? "http://localhost:9200";
            Client = new ElasticClient(new Uri(esUrl));
            var indexSuffix = "_" + DateTime.UtcNow.Ticks;
            Configuration = new ApplicationConfiguration(esUrl,
                "test_jobs" + indexSuffix,
                "test_usersettings" + indexSuffix,
                "test_ownersettings" + indexSuffix,
                "test_reposettings" + indexSuffix,
                "test_datasets" + indexSuffix,
                "test_schemas" + indexSuffix,
                "test_files"  + indexSuffix
                );
            WorkerConfiguration = new WorkerConfiguration(esUrl,
                "test_jobs" + indexSuffix,
                "test_usersettings" + indexSuffix,
                "test_ownersettings" + indexSuffix,
                "test_reposettings" + indexSuffix,
                "test_datasets" + indexSuffix,
                "test_schemas" + indexSuffix,
                "test_files" + indexSuffix,
                "",
                "test_repos" + indexSuffix,
                "datadock_test");
        }

        public void Dispose()
        {
            if (Client.IndexExists(Configuration.DatasetIndexName).Exists) Client.DeleteIndex(Configuration.DatasetIndexName);
            if (Client.IndexExists(Configuration.JobsIndexName).Exists) Client.DeleteIndex(Configuration.JobsIndexName);
            if (Client.IndexExists(Configuration.OwnerSettingsIndexName).Exists)
                Client.DeleteIndex(Configuration.OwnerSettingsIndexName);
            if (Client.IndexExists(Configuration.RepoSettingsIndexName).Exists) Client.DeleteIndex(Configuration.RepoSettingsIndexName);
            if (Client.IndexExists(Configuration.SchemaIndexName).Exists) Client.DeleteIndex(Configuration.SchemaIndexName);
            if (Client.IndexExists(Configuration.UserIndexName).Exists) Client.DeleteIndex(Configuration.UserIndexName);
            if (Directory.Exists(Configuration.FileStorePath)) Directory.Delete(Configuration.FileStorePath, true);
        }
    }
}
