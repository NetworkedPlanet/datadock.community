﻿using Datadock.Common.Stores;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataDock.Web.ViewComponents
{
    [ViewComponent(Name = "Datasets")]
    public class DatasetsViewComponent : ViewComponent
    {
        private readonly IDatasetStore _datasetStore;
        public DatasetsViewComponent(IDatasetStore datasetStore)
        {
            _datasetStore = datasetStore;
        }
        
        public async Task<IViewComponentResult> InvokeAsync(string selectedOwnerId, string selectedRepoId)
        {
            try
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
            catch (Exception e)
            {
                return View("Error", e);
            }
          
        }

        private async Task<List<DatasetViewModel>> GetOwnerDatasets(string selectedOwnerId)
        {
            try
            {
                var datasets = await _datasetStore.GetDatasetsForOwner(selectedOwnerId, 0, 20);
                var datasetViewModels = datasets.Select(d => new DatasetViewModel(d)).ToList();
                return datasetViewModels;
            }
            catch (DatasetNotFoundException dnf)
            {
                return new List<DatasetViewModel>();
            }
            
        }

        private async Task<List<DatasetViewModel>> GetRepoDatasets(string selectedOwnerId, string selectedRepoId)
        {
            try
            {
                var datasets = await _datasetStore.GetDatasetsForRepository(selectedOwnerId, selectedRepoId, 0, 20);
                var datasetViewModels = datasets.Select(d => new DatasetViewModel(d)).ToList();
                return datasetViewModels;
            }
            catch (DatasetNotFoundException dnf)
            {
                return new List<DatasetViewModel>();
            }
            
        }
    }
}
