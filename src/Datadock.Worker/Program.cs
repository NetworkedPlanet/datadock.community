using System;
using System.Threading;
using System.Threading.Tasks;
using Datadock.Common;
using Datadock.Common.Elasticsearch;
using Datadock.Common.Stores;
using DataDock.Common;
using Elasticsearch.Net;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

namespace DataDock.Worker
{
    internal class Program
    {
        private static readonly AutoResetEvent WaitHandle = new AutoResetEvent(false);

        static void Main(string[] args)
        {
            Log.Information("Worker Starting");
            var config = WorkerConfiguration.FromEnvironment();
            config.LogSettings();
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection, config);
            var services = serviceCollection.BuildServiceProvider();
            var application = new Application(services);

            Task.Run(application.Run);

            Console.CancelKeyPress += (o, e) =>
            {
                Console.WriteLine("Exit");
                WaitHandle.Set();
            };

            WaitHandle.WaitOne();
        }

        private static void ConfigureServices(IServiceCollection serviceCollection, WorkerConfiguration config)
        {
            var client = RegisterElasticClient(serviceCollection, config);
            ConfigureLogging(client.ConnectionSettings.ConnectionPool);
            ConfigureServices(serviceCollection, client, config);
        }


        private static IElasticClient RegisterElasticClient(IServiceCollection serviceCollection, ApplicationConfiguration config)
        {
            Log.Information("Attempting to connect to Elasticsearch at {esUrl}", config.ElasticsearchUrl);
            var client = new ElasticClient(new Uri(config.ElasticsearchUrl));
            WaitForElasticsearch(client);
            serviceCollection.AddSingleton<IElasticClient>(client);
            return client;
        }

        private static void WaitForElasticsearch(IElasticClient client)
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

        private static void ConfigureServices(IServiceCollection serviceCollection, IElasticClient elasticClient,
            WorkerConfiguration config)
        {
            serviceCollection.AddSingleton(config);
            serviceCollection.AddSingleton<ApplicationConfiguration>(config);
            serviceCollection.AddScoped<IFileStore, DirectoryFileStore>();

            serviceCollection.AddSingleton<IDatasetStore, DatasetStore>();
            serviceCollection.AddSingleton<IJobStore, JobStore>();
            serviceCollection.AddSingleton<IUserRepository, UserRepository>();
            serviceCollection.AddSingleton<IOwnerSettingsStore,OwnerSettingsStore>();
            serviceCollection.AddSingleton<IRepoSettingsRepository>(
                new RepoSettingsRepository(elasticClient, config.RepoSettingsIndexName));
            serviceCollection.AddSingleton<ISchemaRepository>(
                new SchemaRepository(elasticClient, config.SchemaIndexName));
            serviceCollection.AddSingleton<IProgressLogFactory, SignalrProgressLogFactory>();
            serviceCollection.AddSingleton<IGitHubClientFactory>(
                new GitHubClientFactory(config.GitHubProductHeader));
            serviceCollection.AddSingleton<IQuinceStoreFactory>(new DefaultQuinceStoreFactory());
            serviceCollection.AddTransient<IHtmlGeneratorFactory, HtmlFileGeneratorFactory>();
        }

        private static void ConfigureLogging(IConnectionPool clientConnectionPool)
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
