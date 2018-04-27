using Datadock.Common.Models;
using System.Collections.Generic;
using System.Linq;

namespace DataDock.Web.ViewModels
{
    public class SearchResultViewModel : BaseLayoutViewModel
    {
        public string SearchTag { get; private set; }

        public IReadOnlyList<DatasetViewModel> Results { get; private set; }

        public SearchResultViewModel()
        {
        }

        public SearchResultViewModel(string tag, IEnumerable<DatasetInfo> searchResults)
        {
            SearchTag = tag;
            Results = searchResults.Select(d => new DatasetViewModel(d)).ToList();
        }
    }
}
