using System;
using System.Net;
using Hoverfly.Core;
using Hoverfly.Core.Dsl;
using static Hoverfly.Core.Dsl.HoverflyDsl;
using static Hoverfly.Core.Dsl.ResponseCreators;
using static Hoverfly.Core.Dsl.DslSimulationSource;

namespace DataDock.Web.UI.Tests
{
    public class HoverflyFixture : IDisposable
    {
        private readonly Hoverfly.Core.Hoverfly _hoverfly;

        public int ProxyPort => _hoverfly.GetProxyPort();

        public HoverflyFixture()
        {
            _hoverfly = new Hoverfly.Core.Hoverfly(HoverflyMode.Simulate);
            _hoverfly.Start();
            SetupSimulation();
        }

        private void SetupSimulation()
        {
            _hoverfly.ImportSimulation(
                Dsl(
                    Service("https://github.com")
                        .Get("/")
                        .WillReturn(Success("<h1 class='title'>Awesome</h1>", "text/html"))));
            _hoverfly.ImportSimulation(
                Dsl(
                    Service("https://github.com")
                        .Get("/login/oauth/authorize")
                        .QueryParam("client_id", "test_client_id")
                        .WillReturn(ResponseBuilder.Response()
                            .Status(HttpStatusCode.Redirect)
                            .Header("Location", "http://localhost:5000/signin-github?code=test_code"))));
        }

        public void Dispose()
        {
            if (_hoverfly != null)
            {
                try
                {
                    _hoverfly.Stop();
                }
                catch (TimeoutException) { }

                _hoverfly.Dispose();
            }
        }
    }

}
