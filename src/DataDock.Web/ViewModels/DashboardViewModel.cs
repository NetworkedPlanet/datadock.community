namespace DataDock.Web.ViewModels
{
    public class DashboardViewModel : BaseLayoutViewModel
    {
        public string SelectedOwnerId { get; set; }
        public string SelectedRepoId { get; set; }

        public string UserId { get; set; }

        public string Area { get; set; }

        /// <summary>
        /// Optional SchemaId for when a template has been selected for use on a new import
        /// </summary>
        public string SelectedSchemaId { get; set; }
    }
}
