using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DataDock.Web.Controllers
{
    public class DashboardBaseController : Controller
    {
        public string RequestedOwnerId { get; set; }
        public string RequestedRepoId { get; set; }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            // note - this does not work on postback
            const string key = "ownerId";
            var ownerId = context.ActionArguments.ContainsKey(key) ? context.ActionArguments[key] : "";
            if (ownerId == null)
            {
                context.Result = RedirectToAction("Index", "Home");
                return;
            }
            RequestedOwnerId = ownerId.ToString();

            const string rkey = "repoId";
            var repoId = context.ActionArguments.ContainsKey(rkey) ? context.ActionArguments[rkey] : "";
            RequestedRepoId = repoId.ToString();
        }
        
    }
}
