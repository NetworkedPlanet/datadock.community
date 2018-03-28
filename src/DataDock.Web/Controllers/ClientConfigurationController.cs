using DataDock.Web.Config;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DataDock.Web.Controllers
{
    public class ClientConfigurationController : Controller
    {
        ClientConfiguration _clientConfig;
        public ClientConfigurationController(IOptions<ClientConfiguration> clientConfigOptions)
        {
            _clientConfig = clientConfigOptions?.Value;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return Json(_clientConfig);
        }
    }
}