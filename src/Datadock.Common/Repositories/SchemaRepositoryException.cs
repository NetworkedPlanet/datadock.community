namespace Datadock.Common.Repositories
{
    public class SchemaRepositoryException : DatadockException
    {
        public SchemaRepositoryException(string msg) : base(msg) { }
    }

    public class SchemaNotFoundException : SchemaRepositoryException
    {
        public SchemaNotFoundException(string ownerId, string schemaId) :
            base($"Could not find schema with ID {schemaId} for owner {ownerId}")
        { }
    }
}
