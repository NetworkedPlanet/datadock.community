using System;
using System.Threading;
using System.Threading.Tasks;
using Datadock.Common.Models;
using Datadock.Common.Stores;
using DataDock.Worker.Processors;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using VDS.RDF;

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
            var jobRepo = Services.GetRequiredService<IJobStore>();
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

        private async Task ProcessJob(IJobStore jobStore, JobInfo jobInfo)
        {
            try
            {
                var jobLogger = Log.ForContext("JobId", jobInfo.JobId);
                jobLogger.Information("Processing Job {JobId} - {JobType}", jobInfo.JobId, jobInfo.JobType);

                jobLogger.Debug("Retrieving user account for {UserId}", jobInfo.UserId);
                var userRepo = Services.GetRequiredService<IUserStore>();
                var userAccount = await userRepo.GetUserAccountAsync(jobInfo.UserId);

                jobLogger.Debug("Creating progress log factory");
                var progressLogFactory = Services.GetRequiredService<IProgressLogFactory>();
                var progressLog = await progressLogFactory.MakeProgressLogForJobAsync(jobInfo);
                
                // TODO: Should encapsulate this logic plus basic job info validation into its own processor factory class
                jobLogger.Debug("Creating job processor for job type {JobType}", jobInfo.JobType);
                IDataDockProcessor processor;
                switch (jobInfo.JobType)
                {
                    case JobType.Import:
                    {
                        var cmdProcessorFactory = Services.GetRequiredService<IGitCommandProcessorFactory>();
                        processor = new ImportJobProcessor(
                            Services.GetRequiredService<WorkerConfiguration>(),
                            cmdProcessorFactory.MakeGitCommandProcessor(progressLog),
                            Services.GetRequiredService<IDatasetStore>(),
                            Services.GetRequiredService<IFileStore>(),
                            Services.GetRequiredService<IOwnerSettingsStore>(),
                            Services.GetRequiredService<IRepoSettingsStore>(),
                            Services.GetRequiredService<IDataDockRepositoryFactory>());
                        break;
                    }
                    case JobType.Delete:
                    {
                        var ddRepoFactory = Services.GetRequiredService<IDataDockRepositoryFactory>();
                        var cmdProcessorFactory = Services.GetRequiredService<IGitCommandProcessorFactory>();
                        processor = new DeleteDatasetProcessor(
                            Services.GetRequiredService<WorkerConfiguration>(),
                            cmdProcessorFactory.MakeGitCommandProcessor(progressLog),
                            Services.GetRequiredService<IDatasetStore>(),
                            ddRepoFactory.GetRepositoryForJob(jobInfo, progressLog));
                        break;
                    }
                    case JobType.SchemaCreate:
                        processor = Services.GetRequiredService<ImportSchemaProcessor>();
                        break;
                    case JobType.SchemaDelete:
                        processor = Services.GetRequiredService<DeleteSchemaProcessor>();
                        break;
                    default:
                        throw new WorkerException($"Could not process job of type {jobInfo.JobType}");
                }

                jobLogger.Debug("Start job processor");
                await processor.ProcessJob(jobInfo, userAccount, progressLog);
                jobLogger.Information("Job processing completed");
                jobInfo.CurrentStatus = JobStatus.Completed;
                jobInfo.CompletedAt = DateTime.UtcNow;
                await jobStore.UpdateJobInfoAsync(jobInfo);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Job processing failed for job {JobId}", jobInfo.JobId);
                jobInfo.CurrentStatus = JobStatus.Failed;
                jobInfo.CompletedAt = DateTime.UtcNow;
                await jobStore.UpdateJobInfoAsync(jobInfo);
            }
        }


        
    }
}
