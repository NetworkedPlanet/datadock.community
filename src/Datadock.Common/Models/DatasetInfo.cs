using System;
using System.Collections.Generic;
using Nest;

namespace Datadock.Common.Models
{
    [ElasticsearchType(Name = "datasetinfo", IdProperty = "Id")]
    public class DatasetInfo
    {
        /// <summary>
        /// Combined owner, repo and dataset IDs in the format {ownerId}/{repositoryId}/{datasetId}
        /// </summary>
        [Keyword]
        public string Id { get; set; }

        [Keyword]
        public string OwnerId { get; set; }

        [Keyword]
        public string RepositoryId { get; set; }

        [Keyword]
        public string DatasetId { get; set; }

        [Date]
        public DateTime LastModified { get; set; }

        /// <summary>
        /// CSVW Metadata
        /// </summary>
        public dynamic CsvwMetadata { get; set; }

        /// <summary>
        /// VoID Metadata
        /// </summary>
        public dynamic VoidMetadata { get; set; }

        [Boolean]
        public bool? ShowOnHomePage { get; set; }

        public IEnumerable<string> Tags { get; set; }
    }
}