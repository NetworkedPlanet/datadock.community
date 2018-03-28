using System.Collections.Generic;
using System.Threading.Tasks;
using Datadock.Common.Models;

namespace Datadock.Common.Stores
{
    public interface ISchemaRepository
    {
        /// <summary>
        /// Get a list of schemas for the supplied owners
        /// </summary>
        /// <param name="ownerIds">A list of owner IDs to match</param>
        /// <param name="skip">The number of results to skip</param>
        /// <param name="take">The number of results to return</param>
        /// <returns>A list of <see cref="SchemaInfo"/> instances ordered by last modified date (most recent first)</returns>
        IReadOnlyCollection<SchemaInfo> GetSchemasByOwnerList(string[] ownerIds, int skip, int take);

        /// <summary>
        /// Get a list of schemas for the supplied repositories
        /// </summary>
        /// <param name="repositoryIds">A list of repository IDs to match</param>
        /// <param name="skip">The number of results to skip</param>
        /// <param name="take">The number of results to return</param>
        /// <returns>A list of <see cref="SchemaInfo"/> instances ordered by last modified date (most recent first)</returns>
        IReadOnlyCollection<SchemaInfo> GetSchemasByRepositoryList(string[] repositoryIds, int skip, int take);

        /// <summary>
        /// Get a specific schema by Id
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="schemaId"></param>
        /// <returns></returns>
        Task<SchemaInfo> GetSchemaInfoAsync(string ownerId, string schemaId);

        /// <summary>
        /// Create a new record for a schema
        /// </summary>
        /// <param name="schemaInfo"></param>
        /// <returns></returns>
        Task CreateOrUpdateSchemaRecordAsync(SchemaInfo schemaInfo);

        /// <summary>
        /// Delete all schema records for a given owner
        /// </summary>
        /// <param name="ownerId"></param>
        /// <returns>true if successful</returns>
        Task DeleteSchemaRecordsForOwnerAsync(string ownerId);

        /// <summary>
        /// Delete a specific schema
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="schemaId"></param>
        /// <returns></returns>
        Task DeleteSchemaAsync(string ownerId, string schemaId);
    }
}
