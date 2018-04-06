using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Collections.Generic;
using System.Linq;

namespace DataDock.Web.Routing
{

    public class NonDashboardConstraint : IRouteConstraint
    {
        public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values,
            RouteDirection routeDirection)
        {
            List<string> nonDashboardPages = new List<string>()
            {
                "",
                "search",
                "account",
                "manage",
                "info",
                "dashboard",
                "import",
                "jobs",
                "repositories",
                "datasets",
                "library"
            };
            var ownerId = values["ownerId"].ToString().ToLower();
            // Check for a match (assumes case insensitive)
            var match = nonDashboardPages.Any(x => x.ToLower() == ownerId);
            return !match;
        }

    }
    
}
