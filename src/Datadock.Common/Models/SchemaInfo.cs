using System;
using Newtonsoft.Json;

namespace Datadock.Common.Models
{
    public class SchemaInfo
    {
        public SchemaInfo()
        {
            this.Type = "schema";
        }

        public string Id { get; set; }

        public string Type { get; set; }

        public string OwnerId { get; set; }

        public string RepositoryId { get; set; }

        public string SchemaId { get; set; }

        public DateTime LastModified { get; set; }

        /// <summary>
        /// JSON of schema 
        /// e.g. { dc:title "schema title", metadata: { metadataJson } }
        /// </summary>
        public dynamic Schema { get; set; }

    }
}