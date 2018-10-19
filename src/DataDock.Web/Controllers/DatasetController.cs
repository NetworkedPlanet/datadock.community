using System.Threading.Tasks;
using DataDock.Common.Stores;
using DataDock.Web.Auth;
using DataDock.Web.Routing;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataDock.Web.Controllers
{
    public class DatasetController : DashboardBaseController
    {
        private IDatasetStore _datasetStore;

        public DatasetController(IDatasetStore datasetStore)
        {
            _datasetStore = datasetStore;
        }


        // GET
        [Authorize]
        [ServiceFilter(typeof(AccountExistsFilter))]
        [ServiceFilter(typeof(OwnerAdminAuthFilter))]
        public IActionResult Index(string ownerId, string repositoryId, string datasetId)
        {
            DashboardViewModel.Area = "dataset";
            DashboardViewModel.SelectedDatasetId = datasetId;
            DashboardViewModel.Title = string.Format("{0} > {1} Dataset", DashboardViewModel.SelectedOwnerId,
                DashboardViewModel.SelectedRepoId);
            return View("Dashboard/Dataset", this.DashboardViewModel);
        }
    }
}