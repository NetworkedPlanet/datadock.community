using Datadock.Common.Elasticsearch;
using Datadock.Common.Repositories;
using DataDock.Web.Auth;
using DataDock.Web.Routing;
using DataDock.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SpaServices.Webpack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Datadock.Common.Models;
using Microsoft.EntityFrameworkCore.Storage;
using Octokit;

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
            var esUrl = Environment.GetEnvironmentVariable("ES_URL") ?? "http://elasticsearch:9200";
            var userSettingsIxName = Environment.GetEnvironmentVariable("USERSETTINGS_IX") ?? "usersettings";
            var userAccountIxName = Environment.GetEnvironmentVariable("USERACCOUNT_IX") ?? "useraccount";
            var jobsIxName = Environment.GetEnvironmentVariable("JOBS_IX") ?? "jobs";

            services.AddOptions();

            services.AddMvc();
            services.AddRouting(options =>
            {
                options.LowercaseUrls = true;
            });


            services.AddSignalR();
            var client = new ElasticClient(new Uri(esUrl));

            services.AddScoped<AccountExistsFilter>();

            services.AddSingleton<IElasticClient>(client);
            services.AddSingleton<IUserRepository>(new UserRepository(client, userSettingsIxName, userAccountIxName));
            services.AddSingleton<IJobRepository>(new JobRepository(client, jobsIxName));
            
            services.AddScoped<DataDockCookieAuthenticationEvents>();

            var gitHubClientHeader = Configuration["DataDock:GitHubClientHeader"];
            services.AddSingleton<IGitHubClientFactory>(new GitHubClientFactory(gitHubClientHeader));
            services.AddTransient<IGitHubApiService, GitHubApiService>();

            // services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options => { options.EventsType = typeof(DataDockCookieAuthenticationEvents); });
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = "GitHub";
                })
                .AddCookie(options => {
                    options.LoginPath = "/account/login/";
                    options.LogoutPath = new PathString("/account/logoff/");
                    options.AccessDeniedPath = "/account/forbidden/";
                })
                .AddOAuth("GitHub", options =>
                {
                    options.ClientId = Configuration["GitHub:ClientId"];
                    options.ClientSecret = Configuration["GitHub:ClientSecret"];
                    options.CallbackPath = new PathString("/signin-github");

                    options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
                    options.TokenEndpoint = "https://github.com/login/oauth/access_token";
                    options.UserInformationEndpoint = "https://api.github.com/user";

                    options.Scope.Clear();
                    options.Scope.Add("read:org");
                    options.Scope.Add("public_repo");

                    options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
                    options.ClaimActions.MapJsonKey(ClaimTypes.Name, "login");
                    options.ClaimActions.MapJsonKey(DataDockClaimTypes.GitHubId, "id");
                    options.ClaimActions.MapJsonKey(DataDockClaimTypes.GitHubLogin, "login");
                    options.ClaimActions.MapJsonKey(DataDockClaimTypes.GitHubName, "name");
                    options.ClaimActions.MapJsonKey(DataDockClaimTypes.GitHubUrl, "html_url");
                    options.ClaimActions.MapJsonKey(DataDockClaimTypes.GitHubAvatar, "avatar_url");

                    options.Events = new OAuthEvents
                    {
                        OnCreatingTicket = async context =>
                        {
                            var request =
                                new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                            request.Headers.Authorization =
                                new AuthenticationHeaderValue("Bearer", context.AccessToken);

                            var response = await context.Backchannel.SendAsync(request,
                                HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
                            response.EnsureSuccessStatusCode();

                            var user = JObject.Parse(await response.Content.ReadAsStringAsync());

                            context.RunClaimActions(user);
                            context.Identity.AddClaim(new Claim(DataDockClaimTypes.GitHubAccessToken, context.AccessToken));
                            

                            // check if authorized user exists in DataDock
                            var login = user["login"];
                            await AddOrganizationClaims(context, login.ToString());
                            await EnsureUser(context, login.ToString());
                        }
                    };
                });

            var admins = Configuration["Admin:Logins"]?.Split(",");
            services.AddAuthorization(options =>
            {
                options.AddPolicy("User", policy => policy.RequireClaim(DataDockClaimTypes.DataDockUserId));
                if (admins != null)
                {
                    options.AddPolicy("Admin", policy => policy.RequireClaim(DataDockClaimTypes.GitHubLogin, admins));
                }
                
            });
        }

        private async Task EnsureUser(OAuthCreatingTicketContext context, string login)
        {
            if (string.IsNullOrEmpty(login)) return;
            var userRepository = context.HttpContext.RequestServices.GetService<IUserRepository>();
            try
            {
                var existingAccount = await userRepository.GetUserAccountAsync(login.ToString());
                if (existingAccount != null)
                {
                    context.Identity.AddClaim(new Claim(DataDockClaimTypes.DataDockUserId, login));
                }
            }
            catch (UserAccountNotFoundException notFound)
            {
                // user not found. no action required
            }
        }

        private async Task AddOrganizationClaims(OAuthCreatingTicketContext context, string login)
        {
            if (string.IsNullOrEmpty(login)) return;
            var gitHubApiService = context.HttpContext.RequestServices.GetService<IGitHubApiService>();
            if (gitHubApiService == null)
            {
                Log.Error("Unable to instantiate the GitHub API service");
                return;
            }

            var orgs = await gitHubApiService.GetOrganizationsForUserAsync(login, context.Identity);
            if (orgs != null)
            {
                foreach (Organization org in orgs)
                {
                    var json = JObject.FromObject(new {ownerId = org.Login, avatarUrl = org.AvatarUrl});
                    context.Identity.AddClaim(new Claim(DataDockClaimTypes.GitHubUserOrganization, json.ToString()));
                }
            }
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

            app.UseAuthentication();
            
            app.UseSignalR(routes => routes.MapHub<ProgressHub>("/progress"));
            
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                name: "OwnerProfile",
                template: "{ownerId}",
                defaults: new { controller = "Owner", action = "Index" },
                constraints: new { ownerId = new NonDashboardConstraint() }
            );
                routes.MapRoute(
                    name: "ProxyPortal",
                    template: "Proxy/{ownerId}",
                    defaults: new { controller = "Proxy", action = "Index" },
                    constraints: new { ownerId = new NonDashboardConstraint() }
                );
                routes.MapRoute(
                    name: "OwnerDatasets",
                    template: "{ownerId}/datasets",
                    defaults: new { controller = "Owner", action = "Datasets" },
                    constraints: new { ownerId = new NonDashboardConstraint() }
                );
                routes.MapRoute(
                    name: "OwnerJobs",
                    template: "{ownerId}/jobs",
                    defaults: new { controller = "Owner", action = "Jobs" },
                    constraints: new { ownerId = new NonDashboardConstraint() }
                );
                routes.MapRoute(
                    name: "OwnerLibrary",
                    template: "{ownerId}/library",
                    defaults: new { controller = "Owner", action = "Library" },
                    constraints: new { ownerId = new NonDashboardConstraint() }
                );
                routes.MapRoute(
                    name: "OwnerDeleteSchema",
                    template: "{ownerId}/library/{schemaId}/delete",
                    defaults: new { controller = "Owner", action = "DeleteSchema" },
                    constraints: new { ownerId = new NonDashboardConstraint() }
                );
                routes.MapRoute(
                    name: "OwnerImport",
                    template: "{ownerId}/import/{schemaId?}",
                    defaults: new { controller = "Owner", action = "Import"},
                    constraints: new { ownerId = new NonDashboardConstraint() }
                );
                routes.MapRoute(
                    name: "OwnerSettings",
                    template: "{ownerId}/settings",
                    defaults: new { controller = "Owner", action = "Settings" },
                    constraints: new { ownerId = new NonDashboardConstraint() }
                );
                routes.MapRoute(
                    name: "OwnerAccount",
                    template: "{ownerId}/account",
                    defaults: new { controller = "Owner", action = "Account" },
                    constraints: new { ownerId = new NonDashboardConstraint() }
                );
                routes.MapRoute(
                    name: "OwnerAccountReset",
                    template: "{ownerId}/account/reset",
                    defaults: new { controller = "Owner", action = "ResetToken" },
                    constraints: new { ownerId = new NonDashboardConstraint() }
                );
                routes.MapRoute(
                    name: "OwnerAccountDelete",
                    template: "{ownerId}/account/delete",
                    defaults: new { controller = "Owner", action = "DeleteAccount" },
                    constraints: new { ownerId = new NonDashboardConstraint() }
                );
                routes.MapRoute(
                    name: "OwnerStats",
                    template: "{ownerId}/stats",
                    defaults: new { controller = "Owner", action = "Stats" },
                    constraints: new { ownerId = new NonDashboardConstraint() }
                );
                routes.MapRoute(
                    name: "OwnerSchemas",
                    template: "{ownerId}/schemas",
                    defaults: new { controller = "Owner", action = "Schemas" },
                    constraints: new { ownerId = new NonDashboardConstraint() }
                );

                routes.MapRoute(
                    name: "PfWebhooks",
                    template: "{ownerId}/webhooks",
                    defaults: new { controller = "Owner", action = "Webhooks" },
                    constraints: new { ownerId = new NonDashboardConstraint() }
                );
                routes.MapRoute(
                    name: "PfDomains",
                    template: "{ownerId}/domains",
                    defaults: new { controller = "Owner", action = "Domains" },
                    constraints: new { ownerId = new NonDashboardConstraint() }
                );
                routes.MapRoute(
                    name: "PfValidation",
                    template: "{ownerId}/validation",
                    defaults: new { controller = "Owner", action = "Validation" },
                    constraints: new { ownerId = new NonDashboardConstraint() }
                );
                routes.MapRoute(
                    name: "PfAnalytics",
                    template: "{ownerId}/analytics",
                    defaults: new { controller = "Owner", action = "Analytics" },
                    constraints: new { ownerId = new NonDashboardConstraint() }
                );
                routes.MapRoute(
                    name: "PfVisualizations",
                    template: "{ownerId}/visualizations",
                    defaults: new { controller = "Owner", action = "Visualizations" },
                    constraints: new { ownerId = new NonDashboardConstraint() }
                );

                routes.MapRoute(
                    name: "RepoSummary",
                    template: "{ownerId}/{repoId}",
                    defaults: new { controller = "Repository", action = "Index" },
                    constraints: new { ownerId = new NonDashboardConstraint(), repoId = new PremiumFeatureConstraint() }
                );
                routes.MapRoute(
                    name: "RepoDatasets",
                    template: "{ownerId}/{repoId}/datasets",
                    defaults: new { controller = "Repository", action = "Datasets" },
                    constraints: new { ownerId = new NonDashboardConstraint(), repoId = new PremiumFeatureConstraint() }
                );
                routes.MapRoute(
                    name: "RepoJobs",
                    template: "{ownerId}/{repoId}/jobs",
                    defaults: new { controller = "Repository", action = "Jobs" },
                    constraints: new { ownerId = new NonDashboardConstraint(), repoId = new PremiumFeatureConstraint() }
                );
                routes.MapRoute(
                    name: "RepoLibrary",
                    template: "{ownerId}/{repoId}/library",
                    defaults: new { controller = "Repository", action = "Library" },
                    constraints: new { ownerId = new NonDashboardConstraint(), repoId = new PremiumFeatureConstraint() }
                );
                routes.MapRoute(
                    name: "RepoImport",
                    template: "{ownerId}/{repoId}/import/{schemaId?}",
                    defaults: new { controller = "Repository", action = "Import"},
                    constraints: new { ownerId = new NonDashboardConstraint(), repoId = new PremiumFeatureConstraint() }
                );
                routes.MapRoute(
                    name: "RepoSettings",
                    template: "{ownerId}/{repoId}/settings",
                    defaults: new { controller = "Repository", action = "Settings" },
                    constraints: new { ownerId = new NonDashboardConstraint(), repoId = new PremiumFeatureConstraint() }
                );
                routes.MapRoute(
                    name: "RepoStats",
                    template: "{ownerId}/{repoId}/stats",
                    defaults: new { controller = "Repository", action = "Stats" },
                    constraints: new { ownerId = new NonDashboardConstraint(), repoId = new PremiumFeatureConstraint() }
                );
                routes.MapRoute(
                    name: "Dataset",
                    template: "{ownerId}/{repoId}/{datasetId}/view",
                    defaults: new { controller = "Repository", action = "Dataset" },
                    constraints: new { ownerId = new NonDashboardConstraint(), repoId = new PremiumFeatureConstraint() }
                );
                routes.MapRoute(
                    name: "DeleteDataset",
                    template: "{ownerId}/{repoId}/{datasetId}/delete",
                    defaults: new { controller = "Repository", action = "DeleteDataset" },
                    constraints: new { ownerId = new NonDashboardConstraint(), repoId = new PremiumFeatureConstraint() }
                );
                routes.MapRoute(
                    name: "SignUp",
                    template: "account/signup",
                    defaults: new { controller = "Account", action = "SignUp" });
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
