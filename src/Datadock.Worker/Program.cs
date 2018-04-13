using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

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
            var startup = new Startup();
            startup.ConfigureServices(serviceCollection, config);
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
    }
}
