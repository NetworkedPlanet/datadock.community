using Datadock.Common.Models;
using Datadock.Common.Repositories;
using Microsoft.AspNet.SignalR.Client;

namespace DataDock.Worker
{
    public class SignalrProgressLogFactory : IProgressLogFactory
    {
        private readonly IJobRepository _jobRepository;
        private readonly IHubProxy _hubProxy;

        public SignalrProgressLogFactory(IJobRepository jobRepository, IHubProxy hubProxy)
        {
            _jobRepository = jobRepository;
            _hubProxy = hubProxy;
        }

        public IProgressLog MakeProgressLogForJob(JobInfo job)
        {
            return new SignalrProgressLog(job, _jobRepository, _hubProxy);
        }
    }
}
