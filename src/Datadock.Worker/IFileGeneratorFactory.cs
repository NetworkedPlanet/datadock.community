using System;
using System.Collections.Generic;
using NetworkedPlanet.Quince;

namespace DataDock.Worker
{
    public interface IFileGeneratorFactory
    {
        ITripleCollectionHandler MakeRdfFileGenerator(
            IResourceFileMapper resourceMap,
            IEnumerable<Uri> graphFilter,
            IProgressLog progressLog,
            int reportInterval);

        IResourceStatementHandler MakeHtmlFileGenerator(IResourceFileMapper resourceMap, IViewEngine viewEngine, IProgressLog progressLog, int reportInterval);
    }
}
