using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataDock.Common
{
    public class DataDockUrlHelper
    {
        public const string PublishSite = "http://datadock.io/";
        public static readonly Regex IdentifierRegex = new Regex(@"^http://datadock.io/([^/]+)/([^/]+)/id/(.*)");

        public static string GetRepositoryUri(string repositoryId)
        {
            return PublishSite + repositoryId + "/";
        }

        public static string GetIdentifierPrefix(string repositoryId)
        {
            return PublishSite + repositoryId + "/id/";
        }

        public static string GetDatasetIdentifier(string repositoryId, string datasetId)
        {
            return PublishSite + repositoryId + "/id/dataset/" + datasetId;
        }


    }
}
