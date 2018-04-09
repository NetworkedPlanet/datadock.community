using Octokit;

namespace DataDock.Web.Models
{
    public class RepositoryInfo
    {
        public string OwnerId { get; set; }
        public string RepoId { get; set; }
        public string DataDockImportUrl { get; set; }
        public string OwnerAvatar { get; set; }

        public RepositoryInfo()
        {

        }

        public RepositoryInfo(Repository r)
        {
            this.OwnerId = r.Owner?.Login;
            this.RepoId = r.Name;
            this.DataDockImportUrl = $"/{OwnerId}/{RepoId}/import";
            this.OwnerAvatar = r.Owner?.AvatarUrl;
        }
    }
}
