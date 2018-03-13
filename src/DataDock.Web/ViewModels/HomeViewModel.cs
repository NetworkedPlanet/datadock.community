using Datadock.Common.Models;
using System.Collections.Generic;
using System.Linq;

namespace DataDock.Web.ViewModels
{
    public class HomeViewModel
    {
        public IReadOnlyList<DatasetViewModel> RecentDatasets { get; }

        public HomeViewModel(IEnumerable<DatasetInfo> recentDatasets)
        {
            RecentDatasets = recentDatasets?.Select(ds => new DatasetViewModel(ds)).ToList();
        }
    }
}
