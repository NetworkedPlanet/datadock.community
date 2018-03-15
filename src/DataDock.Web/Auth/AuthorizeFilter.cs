using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DataDock.Web.Auth
{
    public class AuthorizeFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.HttpContext.User != null)
            {
                var user = context.HttpContext.User;
                if (user.Identity.Name != null && user.Identity.IsAuthenticated && !user.ClaimExists(DataDockClaimTypes.DataDockUserId))
                {
                    context.Result = new RedirectToRouteResult("SignUp", null);
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // n/a
        }
    }
}
