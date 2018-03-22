using System;
using Nest;

namespace Datadock.Common.Models
{
    [ElasticsearchType(Name = "schema", IdProperty = "Id")]
    public class SchemaInfo
    {
        [Keyword]
        public string Id { get; set; }

        [Keyword]
        public string OwnerId { get; set; }

        [Keyword]
        public string RepositoryId { get; set; }

        [Keyword]
        public string SchemaId { get; set; }

        [Date]
        public DateTime LastModified { get; set; }

        /// <summary>
        /// JSON of schema 
        /// e.g. { dc:title "schema title", metadata: { metadataJson } }
        /// </summary>
        [Object(Enabled = false)]
        public dynamic Schema { get; set; }

    }
}