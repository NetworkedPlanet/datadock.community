using NetworkedPlanet.Quince;

namespace Datadock.Worker
{
    public interface IHtmlGeneratorFactory
    {
        IResourceStatementHandler MakeHtmlFileGenerator(IResourceFileMapper resourceMap, IViewEngine viewEngine, IProgressLog progressLog, int reportInterval);
    }
}
