namespace Datadock.Common.Stores
{
    public class SchemaStoreException : DatadockException
    {
        public SchemaStoreException(string msg) : base(msg) { }
    }

    public class SchemaNotFoundException : SchemaStoreException
    {
        public SchemaNotFoundException(string ownerId, string schemaId) :
            base($"Could not find schema with ID {schemaId} for owner {ownerId}")
        { }
    }
}
