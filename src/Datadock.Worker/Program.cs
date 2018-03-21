using System;
using System.Threading;
using System.Threading.Tasks;
using Datadock.Common.Elasticsearch;
using Datadock.Common.Models;
using Datadock.Common.Repositories;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

namespace DataDock.Worker
{
    class Program
    {
        private static readonly AutoResetEvent WaitHandle = new AutoResetEvent(false);
        private static ElasticClient _client;

        static void Main(string[] args)
        {
            Log.Information("Worker Starting");
            var config = WorkerConfiguration.FromEnvironment();
            config.LogSettings();
            var serviceCollection = new ServiceCollection();
            var application = new Application(serviceCollection, config);

            Task.Run(application.Run);

            Console.CancelKeyPress += (o, e) =>
            {
                Console.WriteLine("Exit");
                WaitHandle.Set();
            };

            WaitHandle.WaitOne();
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
