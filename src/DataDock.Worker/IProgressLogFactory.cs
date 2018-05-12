using System.Threading.Tasks;
using Datadock.Common.Models;

namespace DataDock.Worker
{
    public interface IProgressLogFactory
    {
        Task<IProgressLog> MakeProgressLogForJobAsync(JobInfo job);
    }
}
