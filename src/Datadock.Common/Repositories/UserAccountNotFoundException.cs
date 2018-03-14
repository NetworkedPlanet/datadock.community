namespace Datadock.Common.Repositories
{
    public class UserAccountNotFoundException : UserRepositoryException
    {
        public UserAccountNotFoundException(string userId) : base($"Could not find account record for user {userId}")
        {
        }
    }
}