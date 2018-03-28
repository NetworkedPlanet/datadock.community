using Datadock.Common.Models;
using Datadock.Common.Stores;
using Microsoft.AspNet.SignalR.Client;

namespace DataDock.Worker
{
    public class SignalrProgressLogFactory : IProgressLogFactory
    {
        private readonly IJobStore _jobRepository;
        private readonly IHubProxy _hubProxy;

        public SignalrProgressLogFactory(IJobStore jobRepository, IHubProxy hubProxy)
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
