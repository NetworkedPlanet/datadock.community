using System.Collections.Generic;
using Datadock.Common.Models;
using DataDock.Web.Models;
using System.Linq;
using System.Security.Claims;
using Newtonsoft.Json;

namespace DataDock.Web.Auth
{
    public class ClaimsHelper
    {
        public Owner GetUserOwnerFromClaims(ClaimsIdentity identity)
        {
            if (identity == null) return null;
            var ghLoginClaim = identity.Claims.FirstOrDefault(c => c.Type.Equals(DataDockClaimTypes.GitHubLogin));
            var ghAvatarClaim = identity.Claims.FirstOrDefault(c => c.Type.Equals(DataDockClaimTypes.GitHubAvatar));
            if (ghLoginClaim != null)
            {
                var owner = new Owner {OwnerId = ghLoginClaim.Value};
                if (ghAvatarClaim != null)
                {
                    owner.AvatarUrl = ghAvatarClaim.Value;
                }

                return owner;
            }

            return null;
        }

        public List<Owner> GetOrgOwnersFromClaims(ClaimsIdentity identity)
        {
            if (identity == null) return null;
            var ghOrgClaims = identity.Claims.Where(c => c.Type.Equals(DataDockClaimTypes.GitHubUserOrganization));
            var orgOwners = new List<Owner>();
            if (ghOrgClaims == null) return new List<Owner>();
            foreach (var claim in ghOrgClaims)
            {
                orgOwners.Add(JsonConvert.DeserializeObject<Owner>(claim.Value));
            }
            return orgOwners;
        }
    }
}
