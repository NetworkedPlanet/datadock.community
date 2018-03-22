using System;
using System.Collections.Generic;
using System.Text;
using Datadock.Common.Elasticsearch;
using Datadock.Common.Models;
using Nest;

namespace DataDock.IntegrationTests
{
    public class ElasticsearchFixture : IDisposable
    {
        public string UserAccountsIndexName { get; }
        public string UserSettingsIndexName { get; }

        public string JobsIndexName { get; }
        public string SchemasIndexName { get; }

        public ElasticClient Client { get; }

        public ElasticsearchFixture()
        {
            var esUrl = Environment.GetEnvironmentVariable("ELASTICSEARCH_URL") ?? "http://localhost:9200";
            Client = new ElasticClient(new Uri(esUrl));
            var indexSuffix = "_" + DateTime.UtcNow.Ticks;
            UserAccountsIndexName = "test_useracccounts" + indexSuffix;
            UserSettingsIndexName = "test_usersettings" + indexSuffix;
            JobsIndexName = "test_jobs" + indexSuffix;
            SchemasIndexName = "test_schemas" + indexSuffix;

            Client.ConnectionSettings.DefaultIndices[typeof(JobInfo)] = JobsIndexName;
        }

        public void Dispose()
        {
            if (Client.IndexExists(UserAccountsIndexName).Exists) Client.DeleteIndex(UserAccountsIndexName);
            if (Client.IndexExists(UserSettingsIndexName).Exists) Client.DeleteIndex(UserSettingsIndexName);
            if (Client.IndexExists(JobsIndexName).Exists) Client.DeleteIndex(JobsIndexName);
            //if (Client.IndexExists(SchemasIndexName).Exists) Client.DeleteIndex(SchemasIndexName);
        }
    }
}
