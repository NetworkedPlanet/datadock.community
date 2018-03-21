using Datadock.Common.Models;

namespace DataDock.Web.ViewModels
{
    public class RepoSettingsViewModel : SettingsViewModel
    {
        public string RepoId { get; set; }
        public string OwnerRepositoryId { get; set; }

        public RepoSettingsViewModel()
        {
        }
        
        public RepoSettingsViewModel(RepoSettings repoSettings)
        {
            OwnerId = repoSettings.OwnerId;
            OwnerIsOrg = repoSettings.OwnerIsOrg;
            OwnerRepositoryId = repoSettings.RepositoryId;
            DefaultPublisherName = repoSettings.DefaultPublisher?.Label;
            DefaultPublisherType = repoSettings.DefaultPublisher?.Type;
            DefaultPublisherEmail = repoSettings.DefaultPublisher?.Email;
            DefaultPublisherWebsite = repoSettings.DefaultPublisher?.Website;
            SearchButtons = repoSettings.SearchButtons;
            LastModifiedBy = repoSettings.LastModifiedBy;
            LastModified = repoSettings.LastModified;
        }

        public RepoSettings AsOwnerSettings()
        {
            return new RepoSettings()
            {
                OwnerId = this.OwnerId,
                OwnerIsOrg = this.OwnerIsOrg,
                RepositoryId = this.OwnerRepositoryId,
                DefaultPublisher = new ContactInfo
                {
                    Label = this.DefaultPublisherName,
                    Type = this.DefaultPublisherType,
                    Email = this.DefaultPublisherEmail,
                    Website = this.DefaultPublisherWebsite
                },
                SearchButtons = this.SearchButtons,
                LastModifiedBy = this.LastModifiedBy,
                LastModified = this.LastModified
            };
        }
    }
}
