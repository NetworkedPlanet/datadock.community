using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NetworkedPlanet.Quince;

namespace DataDock.Worker
{
    public class DefaultQuinceStoreFactory : IQuinceStoreFactory
    {
        private readonly string _quinceSubDir;
        private readonly int _cacheThreshold;

        public DefaultQuinceStoreFactory(string quinceSubDir = "quince", int cacheThreshold = 10)
        {
            _quinceSubDir = quinceSubDir;
            _cacheThreshold = cacheThreshold;
        }

        public IQuinceStore MakeQuinceStore(string repoDirectoryPath)
        {
            var quincePath = Path.Combine(repoDirectoryPath, _quinceSubDir);
            return new DynamicFileStore(quincePath, _cacheThreshold);
        }
    }
}
