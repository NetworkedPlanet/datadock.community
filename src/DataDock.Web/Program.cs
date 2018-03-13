using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DataDock.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((webHostBuilderContext, configurationbuilder) =>
                {
                    var environment = webHostBuilderContext.HostingEnvironment;
                    var oauthSettingsFile = Path.Combine(environment.ContentRootPath, string.Format("oauth.{0}.json", environment.EnvironmentName.ToLower()));
                    configurationbuilder
                        .AddJsonFile("appSettings.json", optional: true)
                        .AddJsonFile(oauthSettingsFile, optional: true);

                    configurationbuilder.AddEnvironmentVariables();
                })
                .UseStartup<Startup>()
                .Build();
    }
}
