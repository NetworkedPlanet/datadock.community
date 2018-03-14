using NetworkedPlanet.Quince;

namespace DataDock.Worker
{
    public class HtmlFileGeneratorFactory : IHtmlGeneratorFactory
    {
        public IResourceStatementHandler MakeHtmlFileGenerator(IResourceFileMapper resourceMap, IViewEngine viewEngine, IProgressLog progressLog, int reportInterval)
        {
            return new HtmlFileGenerator(resourceMap, viewEngine, progressLog, reportInterval);
        }
    }
}