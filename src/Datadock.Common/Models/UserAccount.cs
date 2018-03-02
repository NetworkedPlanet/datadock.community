using System.Collections.Generic;
using Nest;

namespace Datadock.Common.Models
{
    [ElasticsearchType(Name = "useraccount", IdProperty = "UserId")]
    public class UserAccount
    {
        [Keyword]
        public string UserId { get; set; }

        public List<AccountClaim> Claims { get; set; }

    }
}
