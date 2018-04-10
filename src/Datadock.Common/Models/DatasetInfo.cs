using System;

namespace Datadock.Common.Models
{
    public class DatasetInfo
    {
        public DatasetInfo()
        {
            this.Type = "dataset";
        }

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
    }
}