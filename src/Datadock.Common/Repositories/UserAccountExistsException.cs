namespace Datadock.Common.Repositories
{
    public class UserAccountExistsException : UserRepositoryException
    {
        public UserAccountExistsException(string userId):base($"An account already exists for user {userId}") { }
    }
}