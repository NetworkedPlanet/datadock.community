namespace Datadock.Common.Stores
{
    public class JobNotFoundException : JobRepositoryException
    {
        public JobNotFoundException(string jobId) : base("No job found with id " + jobId) { }
    }
}