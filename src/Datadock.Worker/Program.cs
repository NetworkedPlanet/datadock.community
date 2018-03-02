using System;
using System.Threading;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

namespace Datadock.Worker
{
    class Program
    {
        private static readonly AutoResetEvent waitHandle = new AutoResetEvent(false);
        private HubConnection _hubConnection;

        static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                var client = new ElasticLowLevelClient(new ConnectionConfiguration(
                    new Uri("http://elasticsearch:9200")));
                var elasticsearchConnected = false;
                while (!elasticsearchConnected)
                {
                    var response = client.Ping<StringResponse>();
                    if (response.HttpStatusCode == 200)
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

                Log.Logger = new LoggerConfiguration().Enrich.FromLogContext()
                    .MinimumLevel.Debug()
                    .WriteTo.Elasticsearch().WriteTo.Elasticsearch(
                        new ElasticsearchSinkOptions(new Uri("http://elasticsearch:9200"))
                        {
                            MinimumLogEventLevel = LogEventLevel.Debug,
                            AutoRegisterTemplate = true
                        })
                    .CreateLogger();
                var hubConnection = new HubConnectionBuilder().WithUrl("http://datadock.web/progress").Build();
                hubConnection.Closed += (e) => { Console.WriteLine("Connection was closed"); };
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

                while (true)
                {
                    Log.Information("ProgressUpdate {UserId}: {JobId}: {ProgressMessage}", "userId", "jobId", "This is a test message");
                    await hubConnection.SendAsync("ProgressUpdated", "userId", "jobId", "This is a test message");
                    Thread.Sleep(1000);
                }
            });

            Console.CancelKeyPress += (o, e) =>
            {
                Console.WriteLine("Exit");
                waitHandle.Set();
            };

            waitHandle.WaitOne();
        }
    }
}
