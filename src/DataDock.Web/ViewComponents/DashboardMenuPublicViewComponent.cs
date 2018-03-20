using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DataDock.Web.ViewComponents
{
    [ViewComponent(Name = "DashboardMenuPublic")]
    public class DashboardMenuPublicViewComponent : ViewComponent
    {
        protected DashboardMenuPublicViewComponent()
        {
        }

        public async Task<IViewComponentResult> InvokeAsync(string currentPage)
        {
            return View();
        }
    }
}
