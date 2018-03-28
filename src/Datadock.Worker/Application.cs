using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Datadock.Common.Elasticsearch;
using Datadock.Common.Models;
using Datadock.Common.Stores;
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

        public Application(IServiceProvider services)
        {
            Services = services;
        }

        public async Task Run()
        {
            Log.Information("Initializing SignalR hub connection");
            await InitializeHubConnection();
            var jobRepo = Services.GetRequiredService<IJobStore>();
            while (true)
            {
                Thread.Sleep(1000);
                var job = await jobRepo.GetNextJob();
                if (job != null)
                {
                    Log.Information("Found new job: {JobId} {JobType}", job.JobId, job.JobType);
                    await ProcessJob(job);
                }
            }
        }

        private async Task ProcessJob(JobInfo jobInfo)
        {
            try
            {
                var userRepo = Services.GetRequiredService<IUserStore>();
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
                    default:
                        throw new WorkerException($"Could not process job of type {jobInfo.JobType}");
                }

                var progressLog = Services.GetRequiredService<IProgressLogFactory>().MakeProgressLogForJob(jobInfo);
                await processor.ProcessJob(jobInfo, userAccount, progressLog);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Job processing failed");
            }
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
