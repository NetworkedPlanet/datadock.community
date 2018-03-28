using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using DataDock.Web.Models;

namespace DataDock.Web.ViewComponents
{
    [ViewComponent(Name = "Datasets")]
    public class DatasetsViewComponent : ViewComponent
    {
        // DatasetStore NotYetImplemented

        //private readonly IDatasetStore _datasetStore;
        //public DatasetsViewComponent(IDatasetStore datasetStore)
        //{
        //    _datasetStore = datasetStore;
        //}

        public DatasetsViewComponent()
        {

        }

        public async Task<IViewComponentResult> InvokeAsync(string selectedOwnerId, string selectedRepoId)
        {
            if (string.IsNullOrEmpty(selectedOwnerId)) return View("Empty");

            if (string.IsNullOrEmpty(selectedRepoId))
            {
                var datasetsList = await GetOwnerDatasets(selectedOwnerId);
                return View(datasetsList);
            }
            var repoDatasetsList = await GetRepoDatasets(selectedOwnerId, selectedRepoId);
            return View(repoDatasetsList);
        }

        private async Task<List<DatasetViewModel>> GetOwnerDatasets(string selectedOwnerId)
        {
            //var datasets = _datasetStore.GetDatasetsForOwner(selectedOwnerId, 0, 20);
            //var datasetViewModels = datasets.Select(d => new DatasetViewModel(d)).ToList();
            //return datasetViewModels;

            return new List<DatasetViewModel>();
        }

        private async Task<List<DatasetViewModel>> GetRepoDatasets(string selectedOwnerId, string selectedRepoId)
        {
            //var ownerRepository = string.Format("{0}/{1}", selectedOwnerId, selectedRepoId);
            //var datasets = _datasetStore.GetDatasetsForRepository(ownerRepository, 0, 20);
            //var datasetViewModels = datasets.Select(d => new DatasetViewModel(d)).ToList();
            //return datasetViewModels;

            return new List<DatasetViewModel>();
        }
    }
}
