namespace Datadock.Common.Stores
{
    public class JobNotFoundException : JobStoreException
    {
        public JobNotFoundException(string jobId) : base("No job found with id " + jobId) { }
    }
}