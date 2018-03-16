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
            List<string> nonDashboardPages = new List<string>() { "", "search", "account", "manage", "info", "proxy", "dashboard", "import", "jobs", "datasets", "library" };
            // Get the username from the url
            var ownerId = values["ownerId"].ToString().ToLower();
            // Check for a match (assumes case insensitive)
            var match = nonDashboardPages.Any(x => x.ToLower() == ownerId);
            return !match;
        }

    }

    public class PremiumFeatureConstraint : IRouteConstraint
    {
        public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values,
            RouteDirection routeDirection)
        {
            List<string> premiumFeaturePages = new List<string>() { "webhooks", "domains", "visualizations", "validation", "analytics" };
            // Get the username from the url
            var repoId = values["repoId"].ToString().ToLower();
            // Check for a match (assumes case insensitive)
            var match = premiumFeaturePages.Any(x => x.ToLower() == repoId);
            return !match;
        }
    }
}
