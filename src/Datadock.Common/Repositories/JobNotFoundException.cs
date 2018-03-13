namespace Datadock.Common.Repositories
{
    public class JobNotFoundException : JobRepositoryException
    {
        public JobNotFoundException(string jobId) : base("No job found with id " + jobId) { }
    }
}