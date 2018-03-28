using Datadock.Common.Models;

namespace DataDock.Worker
{
    public interface IProgressLogFactory
    {
        IProgressLog MakeProgressLogForJob(JobInfo job);
    }
}
