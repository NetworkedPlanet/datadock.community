using System;
using System.Threading;
using System.Threading.Tasks;
using Datadock.Common.Elasticsearch;
using Datadock.Common.Models;
using Datadock.Common.Repositories;
using Elasticsearch.Net;
using Microsoft.AspNetCore.SignalR.Client;
using Nest;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

namespace DataDock.Worker
{
    class Program
    {
        private static readonly AutoResetEvent WaitHandle = new AutoResetEvent(false);
        private static Task[] _processingTasks;
        private static ElasticClient _client;
        private static HubConnection _hubConnection;

        static void Main(string[] args)
        {
            var esUrl = Environment.GetEnvironmentVariable("ES_URL") ?? "http://elasticsearch:9200";
            var jobsIndexName = Environment.GetEnvironmentVariable("JOBS_IX") ?? "jobs";
            
            Log.Information("Worker Starting");
            Log.Information("ES_URL {esUrl}", esUrl);
            Log.Information("JOBS_IX {jobsIx}", jobsIndexName);

            Task.Run(async () =>
            {
                _client = new ElasticClient(new Uri(esUrl));
                Log.Information("Waiting for ES to respond to pings");
                WaitForElasticsearch();
                Log.Information("Reconfiguring logging to go to ES");
                ConfigureLogging();
                Log.Information("Initializing SignalR hub connection");
                _hubConnection = await InitializeHubConnection();
                Log.Information("Initializing JobsRepository: {index}", jobsIndexName);
                var jobRepo = new JobRepository(_client, jobsIndexName);
                while (true)
                {
                    Thread.Sleep(1000);
                    var job = await jobRepo.GetNextJob();
                    if (job != null)
                    {
                        Log.Information("Found new job: {JobId} {JobType}", job.JobId, job.JobType);
                        ProcessJob(jobRepo, job);
                    }
                }
            });

            Console.CancelKeyPress += (o, e) =>
            {
                Console.WriteLine("Exit");
                WaitHandle.Set();
            };

            WaitHandle.WaitOne();
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
            _hubConnection = InitializeHubConnection().Result;
        }

        private static void ConfigureLogging()
        {
            Log.Logger = new LoggerConfiguration().Enrich.FromLogContext()
                .MinimumLevel.Debug()
                .WriteTo.Elasticsearch().WriteTo.Elasticsearch(
                    new ElasticsearchSinkOptions(new Uri("http://elasticsearch:9200"))
                    {
                        MinimumLogEventLevel = LogEventLevel.Debug,
                        AutoRegisterTemplate = true
                    })
                .CreateLogger();
        }

        private static void WaitForElasticsearch()
        {
            var elasticsearchConnected = false;
            while (!elasticsearchConnected)
            {
                var response = _client.Ping();
                if (response.IsValid)
                {
                    Console.WriteLine("Elasticsearch is Running!");
                    elasticsearchConnected = true;
                }
                else
                {
                    Console.WriteLine("Elasticsearch is starting");
                }
                Thread.Sleep(1000);
            }

        }
        private static async void ProcessJob(IJobRepository jobRepo, JobInfo job)
        {
            Log.Information("Start working on job {jobId}", job.JobId);
            
            Thread.Sleep(5000);
            job.RefreshedTimestamp = DateTime.UtcNow.Ticks;
            await jobRepo.UpdateJobInfoAsync(job);

        }
    }

}
