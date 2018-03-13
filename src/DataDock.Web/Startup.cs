using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Datadock.Common.Elasticsearch;
using Datadock.Common.Repositories;
using DataDock.Web.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices.Webpack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nest;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

namespace DataDock.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Elasticsearch(
                    new ElasticsearchSinkOptions(new Uri("http://elasticsearch:9200"))
                    {
                        MinimumLogEventLevel = LogEventLevel.Debug,
                        AutoRegisterTemplate = true
                    })
                .CreateLogger();

        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddSignalR();
            var client = new ElasticClient(new Uri("http://elasticsearch:9200"));
            EnsureElasticsearchIndexes(client);
            services.AddSingleton<IElasticClient>(client);
            services.AddSingleton<IUserRepository>(new UserRepository(client));
        }

        private static void EnsureElasticsearchIndexes(IElasticClient client)
        {
            var elasticsearchAvailable = false;
            while (!elasticsearchAvailable)
            {
                Thread.Sleep(1000);
                elasticsearchAvailable = client.Ping().IsValid;
            }

            EnsureIndex(client, "useraccounts", ElasticsearchMapping.UserAccountIndexMappings);

            EnsureIndex(client, "usersettings", ElasticsearchMapping.UserSettingsIndexMappings);

            // Leave this index as the last one to be created
            EnsureIndex(client, "jobs", ElasticsearchMapping.JobsIndexMappings);
        }

        private static void EnsureIndex(IElasticClient client, string indexName, Func<MappingsDescriptor, IPromise<IMappings>> mappingsPromise)
        {
            var existsResponse = client.IndexExists(indexName);
            if (!existsResponse.Exists)
            {
                var createIndexResponse = client.CreateIndex(indexName, c => c.Mappings(mappingsPromise));
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddSerilog();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebpackDevMiddleware(new WebpackDevMiddlewareOptions
                {
                    HotModuleReplacement = true
                });
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseSignalR(routes => routes.MapHub<ProgressHub>("/progress"));

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");

                routes.MapSpaFallbackRoute(
                    name: "spa-fallback",
                    defaults: new { controller = "Home", action = "Index" });
            });
        }
    }
}
