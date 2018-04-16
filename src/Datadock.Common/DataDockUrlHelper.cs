using System.Text.RegularExpressions;

namespace DataDock.Common
{
    public class DataDockUrlHelper
    {
        public const string PublishSite = "http://datadock.io/";
        public static readonly Regex IdentifierRegex = new Regex(@"^http://datadock.io/([^/]+)/([^/]+)/id/(.*)");

        public static string GetRepositoryUri(string ownerId, string repositoryId)
        {
            return $"{PublishSite}{ownerId}/{repositoryId}/";
        }

        public static string GetIdentifierPrefix(string ownerId, string repositoryId)
        {
            return $"{PublishSite}{ownerId}/{repositoryId}/id/";
        }

        public static string GetDatasetIdentifier(string ownerId, string repositoryId, string datasetId)
        {
            return $"{PublishSite}{ownerId}/{repositoryId}/id/dataset/{datasetId}";
        }
    }
}
