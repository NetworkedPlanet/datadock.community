using System;
using Octokit;

namespace Datadock.Common
{
    public class GitHubClientFactory : IGitHubClientFactory
    {
        private readonly string _productHeaderValue;
        public GitHubClientFactory(string productHeaderValue)
        {
            _productHeaderValue = productHeaderValue;
        }

        public GitHubClient GetClient(string accessToken)
        {
            if (accessToken == null) throw new ArgumentNullException(nameof(accessToken));
            var client = new GitHubClient(new ProductHeaderValue(_productHeaderValue))
            {
                Credentials = new Credentials(accessToken)
            };
            return client;
        }
    }
}