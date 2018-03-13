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

        public dynamic Metadata { get; set; }

        public bool? ShowOnHomePage { get; set; }
    }
}