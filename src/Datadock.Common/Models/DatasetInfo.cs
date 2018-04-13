﻿using System;
using System.Collections.Generic;
using Nest;

namespace Datadock.Common.Models
{
    [ElasticsearchType(Name = "datasetinfo", IdProperty = "FullId")]
    public class DatasetInfo
    {
        public DatasetInfo()
        {
            this.Type = "dataset";
        }

        /// <summary>
        /// Combined owner, repo and dataset IDs in the format {ownerId}/{repositoryId}/{datasetId}
        /// </summary>
        public string FullId { get; set; }

        public string Id { get; set; }

        public string Type { get; set; }

        public string OwnerId { get; set; }

        public string RepositoryId { get; set; }

        public string DatasetId { get; set; }

        public DateTime LastModified { get; set; }

        /// <summary>
        /// CSVW Metadata
        /// </summary>
        public dynamic CsvwMetadata { get; set; }

        /// <summary>
        /// VoID Metadata
        /// </summary>
        public dynamic VoidMetadata { get; set; }

        public bool? ShowOnHomePage { get; set; }

        public IEnumerable<string> Tags { get; set; }
    }
}