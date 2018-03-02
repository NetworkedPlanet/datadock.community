using System.Collections.Generic;
using Datadock.Common.Repositories;
using Nest;

namespace Datadock.Common.Models
{
    [ElasticsearchType(Name = "useraccounts", IdProperty = "UserId")]
    public class UserAccount
    {
        [Keyword]
        public string UserId { get; set; }

        public List<AccountClaim> Claims { get; set; }

    }
}
