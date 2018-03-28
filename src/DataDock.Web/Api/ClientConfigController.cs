using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataDock.Web.Config;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DataDock.Web.Api
{

    [Produces("application/json")]
    [Route("api/clientConfig")]
    public class ClientConfigController : Controller
    {
        private readonly ClientConfiguration _clientConfig;
        public ClientConfigController(IOptions<ClientConfiguration> clientConfigOptions)
        {
            _clientConfig = clientConfigOptions?.Value;
        }

        // GET: api/clientConfig
        [HttpGet]
        public ClientConfiguration Get()
        {
            return _clientConfig;
        }
        
    }
}
