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

namespace Datadock.Worker
{
    class Program
    {
        private static readonly AutoResetEvent WaitHandle = new AutoResetEvent(false);
        private static Task[] _processingTasks;
        private static ElasticClient _client;
        private static HubConnection _hubConnection;

        static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                _client = new ElasticClient(new Uri("http://elasticsearch:9200"));
                WaitForElasticsearch();                
                ConfigureLogging();
                _hubConnection = await InitializeHubConnection();

                var jobRepo = new JobRepository(_client);
                while (true)
                {
                    Log.Information("ProgressUpdate {UserId}: {JobId}: {ProgressMessage}", "userId", "jobId", "This is a test message");
                    await _hubConnection.SendAsync("ProgressUpdated", "userId", "jobId", "This is a test message");
                    Thread.Sleep(1000);

                    var job = await jobRepo.GetNextJob();
                    if (job != null)
                    {
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
