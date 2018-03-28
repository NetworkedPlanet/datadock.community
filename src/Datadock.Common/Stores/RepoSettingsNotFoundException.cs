namespace Datadock.Common.Stores
{
    public class RepoSettingsNotFoundException : JobRepositoryException
    {
        public RepoSettingsNotFoundException(string repositoryId) : base("No repo settings found with repositoryId " + repositoryId) { }
    }
}