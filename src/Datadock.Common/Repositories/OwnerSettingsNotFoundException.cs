namespace Datadock.Common.Repositories
{
    public class OwnerSettingsNotFoundException : JobRepositoryException
    {
        public OwnerSettingsNotFoundException(string ownerId) : base("No owner settings found with ownerId " + ownerId) { }
    }
}