namespace Datadock.Common.Stores
{
    public class DatasetNotFoundException : DatasetStoreException
    {
        public DatasetNotFoundException(string ownerId, string repositoryId = "", string datasetId = "") : base($"No dataset found for owner '{ownerId}', repository: '{repositoryId}', dataset: '{datasetId}' ") { }
    }
}