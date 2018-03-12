using Datadock.Common.Repositories;

namespace Datadock.Common.Elasticsearch
{
    public class UserAccountNotFoundException : UserRepositoryException
    {
        public UserAccountNotFoundException(string userId) : base($"Could not find account record for user {userId}")
        {
        }
    }
}