using System;
using System.Collections.Generic;
using NetworkedPlanet.Quince;

namespace DataDock.Worker
{
    public class FileGeneratorFactory : IFileGeneratorFactory
    {
        public ITripleCollectionHandler MakeRdfFileGenerator(
            IResourceFileMapper resourceMap, 
            IEnumerable<Uri> graphFilter,
            IProgressLog progressLog,
            int reportInterval)
        {
            return new RdfFileGenerator(resourceMap, graphFilter, progressLog, reportInterval);
        }

        public IResourceStatementHandler MakeHtmlFileGenerator(IResourceFileMapper resourceMap, IViewEngine viewEngine, IProgressLog progressLog, int reportInterval)
        {
            return new HtmlFileGenerator(resourceMap, viewEngine, progressLog, reportInterval);
        }
    }
}