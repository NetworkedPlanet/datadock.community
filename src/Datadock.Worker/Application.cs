using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Datadock.Common.Elasticsearch;
using Datadock.Common.Models;
using Datadock.Common.Repositories;
using DataDock.Worker.Processors;
using Elasticsearch.Net;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

namespace DataDock.Worker
{
    public class Application
    {
        private IServiceProvider Services { get; }

        public Application(ServiceCollection serviceCollection, ApplicationConfiguration config)
        {
            var client = GetElasticClient(serviceCollection);
            ConfigureLogging(client.ConnectionSettings.ConnectionPool);
            ConfigureServices(serviceCollection, client, config);
            Services = serviceCollection.BuildServiceProvider();
        }

        public async Task Run()
        {
            Log.Information("Initializing SignalR hub connection");
            await InitializeHubConnection();
            var jobRepo = Services.GetRequiredService<IJobRepository>();
            while (true)
            {
                Thread.Sleep(1000);
                var job = await jobRepo.GetNextJob();
                if (job != null)
                {
                    Log.Information("Found new job: {JobId} {JobType}", job.JobId, job.JobType);
                    await ProcessJob(jobRepo, job);
                }
            }
        }

        private async Task ProcessJob(IJobRepository jobRepository, JobInfo jobInfo)
        {
            try
            {
                var userRepo = Services.GetRequiredService<IUserRepository>();
                var userAccount = await userRepo.GetUserAccountAsync(jobInfo.UserId);

                IDataDockProcessor processor;
                switch (jobInfo.JobType)
                {
                    case JobType.Import:
                        processor = Services.GetRequiredService<ImportJobProcessor>();
                        break;
                    case JobType.Delete:
                        processor = Services.GetRequiredService<DeleteDatasetProcessor>();
                        break;
                    case JobType.SchemaCreate:
                        processor = Services.GetRequiredService<ImportSchemaProcessor>();
                        break;
                    case JobType.SchemaDelete:
                        processor = Services.GetRequiredService<DeleteSchemaProcessor>();
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Job processing failed");
            }
        }

        private static IElasticClient GetElasticClient(IServiceCollection services)
        {
            var esUrl = Environment.GetEnvironmentVariable("ES_URL") ?? "http://elasticsearch:9200";
            var client = new ElasticClient(new Uri(esUrl));
            
            Log.Information("Waiting for ES to respond to pings");
            WaitForElasticsearch(client);
            services.AddSingleton<IElasticClient>(client);
            return client;
        }

        private static void WaitForElasticsearch(IElasticClient client)
        {
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

        private static void ConfigureServices(IServiceCollection services, IElasticClient elasticClient, ApplicationConfiguration config)
        {
            services.AddSingleton<IJobRepository>(new JobRepository(elasticClient, config.JobsIndexName));
            services.AddSingleton<IUserRepository>(new UserRepository(elasticClient, "obsolete", config.UserIndexName));
            services.AddScoped<IProgressLog>(sp => { return new ProgressLog()})
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

        private static async Task<HubConnection> InitializeHubConnection()
        {
            var hubConnection = new HubConnectionBuilder().WithUrl("http://datadock.web/progress").Build();
            hubConnection.Closed += OnHubConnectionLost;
            var connectionStarted = false;
            while (!connectionStarted)
            {
                try
                {
                    await hubConnection.StartAsync();
                    connectionStarted = true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "signalr connection failed");
                    Console.WriteLine("Error connecting to Signalr Hub: " + ex);
                    Thread.Sleep(3000);
                }
            }

            return hubConnection;
        }

        private static void OnHubConnectionLost(Exception exc)
        {
            Log.Warning(exc, "SignalR hub connection was lost. Reconnecting in 3 seconds");
            Thread.Sleep(3000);
            InitializeHubConnection().RunSynchronously();
        }

    }
}
