using Octokit;

namespace DataDock.Web.ViewModels
{
    public class RepositoryInfoViewModel
    {
        public string OwnerId { get; set; }
        public string RepoId { get; set; }
        public string DataDockImportUrl { get; set; }
        public string OwnerAvatar { get; set; }

        public RepositoryInfoViewModel()
        {

        }

        public RepositoryInfoViewModel(Repository r)
        {
            this.OwnerId = r.Owner?.Login;
            this.RepoId = r.Name;
            this.DataDockImportUrl = $"/{OwnerId}/{RepoId}/import";
            this.OwnerAvatar = r.Owner?.AvatarUrl;
        }
    }
}
