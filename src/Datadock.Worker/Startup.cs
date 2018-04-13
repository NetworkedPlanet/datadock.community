using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Datadock.Common;
using Datadock.Common.Elasticsearch;
using Datadock.Common.Stores;
using DataDock.Common;
using Elasticsearch.Net;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using NetworkedPlanet.Quince.Git;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

namespace DataDock.Worker
{
    public class Startup
    {
        public virtual void ConfigureServices(IServiceCollection serviceCollection, WorkerConfiguration config)
        {
            var client = RegisterElasticClient(serviceCollection, config);
            ConfigureLogging(client.ConnectionSettings.ConnectionPool);
            ConfigureServices(serviceCollection, client, config);
        }

        protected IElasticClient RegisterElasticClient(IServiceCollection serviceCollection, ApplicationConfiguration config)
        {
            Log.Information("Attempting to connect to Elasticsearch at {esUrl}", config.ElasticsearchUrl);
            var client = new ElasticClient(new Uri(config.ElasticsearchUrl));
            WaitForElasticsearch(client);
            serviceCollection.AddSingleton<IElasticClient>(client);
            return client;
        }

        protected void WaitForElasticsearch(IElasticClient client)
        {
            Log.Information("Waiting for ES to respond to pings");
            var elasticsearchConnected = false;
            while (!elasticsearchConnected)
            {
                var response = client.Ping();
                if (response.IsValid)
                {
                    Log.Information("Elasticsearch is Running!");
                    elasticsearchConnected = true;
                }
                else
                {
                    Log.Information("Elasticsearch is starting");
                }

                Thread.Sleep(1000);
            }

        }

        protected void ConfigureServices(IServiceCollection serviceCollection, IElasticClient elasticClient,
            WorkerConfiguration config)
        {
            serviceCollection.AddSingleton(config);
            serviceCollection.AddSingleton<ApplicationConfiguration>(config);
            serviceCollection.AddScoped<IFileStore, DirectoryFileStore>();

            serviceCollection.AddSingleton<IDatasetStore, DatasetStore>();
            serviceCollection.AddSingleton<IJobStore, JobStore>();
            serviceCollection.AddSingleton<IUserStore, UserStore>();
            serviceCollection.AddSingleton<IOwnerSettingsStore, OwnerSettingsStore>();
            serviceCollection.AddSingleton<IRepoSettingsStore, RepoSettingsStore>();
            serviceCollection.AddSingleton<ISchemaStore, SchemaStore>();
            serviceCollection.AddSingleton<IProgressLogFactory, SignalrProgressLogFactory>();
            serviceCollection.AddSingleton<IGitHubClientFactory>(new GitHubClientFactory(config.GitHubProductHeader));
            serviceCollection.AddSingleton<IGitWrapperFactory>(new DefaultGitWrapperFactory(config.GitPath));
            serviceCollection.AddSingleton<IQuinceStoreFactory, DefaultQuinceStoreFactory>();
            serviceCollection.AddTransient<IFileGeneratorFactory, FileGeneratorFactory>();
            serviceCollection.AddTransient<IDataDockRepositoryFactory, DataDockRepositoryFactory>();
            serviceCollection.AddSingleton<IGitCommandProcessorFactory, GitCommandProcessorFactory>();
        }

        protected void ConfigureLogging(IConnectionPool clientConnectionPool)
        {
            Log.Logger = new LoggerConfiguration().Enrich.FromLogContext()
                .MinimumLevel.Debug()
                .WriteTo.Elasticsearch().WriteTo.Elasticsearch(
                    new ElasticsearchSinkOptions(clientConnectionPool)
                    {
                        MinimumLogEventLevel = LogEventLevel.Debug,
                        AutoRegisterTemplate = true
                    })
                .CreateLogger();

        }
    }
}
