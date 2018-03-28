namespace Datadock.Common.Stores
{
    public class UserAccountExistsException : UserRepositoryException
    {
        public UserAccountExistsException(string userId):base($"An account already exists for user {userId}") { }
    }
}