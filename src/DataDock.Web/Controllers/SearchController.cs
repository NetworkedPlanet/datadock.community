using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataDock.Common.Stores;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace DataDock.Web.Controllers
{
    public class SearchController : Controller
    {
        private readonly IDatasetStore _datasetStore;

        public SearchController(IDatasetStore datasetStore)
        {
            _datasetStore = datasetStore;
        }

        public async Task<IActionResult> Index(string tag)
        {
            var model = new SearchResultViewModel();
            try
            {
                if (!string.IsNullOrEmpty(tag))
                {
                    var tags = new string[] {tag};
                    // TODO get skip and take
                    var results = await _datasetStore.GetDatasetsForTagsAsync(tags, 0, 25);
                    model = new SearchResultViewModel(tag, results);
                }
            }
            catch (DatasetNotFoundException dnf)
            {
                // none found, no special action needed
            }
            catch (Exception e)
            {
                ViewBag.Error = e;
                Log.Error("Error searching by tag via the search page", e);
            }
            return View(model);
        }
    }
}