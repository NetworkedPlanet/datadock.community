using NetworkedPlanet.Quince;

namespace DataDock.Worker
{
    public interface IHtmlGeneratorFactory
    {
        IResourceStatementHandler MakeHtmlFileGenerator(IResourceFileMapper resourceMap, IViewEngine viewEngine, IProgressLog progressLog, int reportInterval);
    }
}
