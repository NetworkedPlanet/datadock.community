using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DataDock.Web.ViewComponents
{
    [ViewComponent(Name = "DashboardMenuPrivate")]
    public class DashboardMenuPrivateViewComponent : ViewComponent
    {
       
        public DashboardMenuPrivateViewComponent()
        {

        }

        public async Task<IViewComponentResult> InvokeAsync(string currentPage)
        {
            var dmvm = new DashboardMenuPrivateViewModel();
            return View(dmvm);
        }
    }
}
