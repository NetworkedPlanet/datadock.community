using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;

namespace DataDock.Web.Auth
{
    public class ApplicationUser : IPrincipal
    {
        public bool IsInRole(string role)
        {
            throw new NotImplementedException();
        }

        public IIdentity Identity { get; }
    }
}
