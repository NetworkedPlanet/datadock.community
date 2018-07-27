using System;
using System.Collections.Generic;
using System.Text;
using Hoverfly.Core;
using Hoverfly.Core.Resources;
using Xunit;

namespace DataDock.Web.UI.Tests
{
    public class HoverflyFixture : IDisposable
    {
        public Hoverfly.Core.Hoverfly hf;      

        public HoverflyFixture()
        {
           CaptureStart();
        }

        public void Dispose()
        {
            CaptureEnd();
        }

        public void CaptureStart()
        {
            hf = new Hoverfly.Core.Hoverfly(HoverflyMode.Capture);
            hf.Start();
        }

        public void CaptureEnd()
        {
            var timestamp = DateTime.UtcNow.ToString("YYYYmmddHHMMSS");
            hf.ExportSimulation(new FileSimulationSource($"test_{timestamp}_simulation.json"));
            hf.Stop();
        }

        public void SimulateStart()
        {
            hf = new Hoverfly.Core.Hoverfly(HoverflyMode.Simulate);
            var timestamp = DateTime.UtcNow.ToString("YYYYmmddHHMMSS");
            hf.ImportSimulation(new FileSimulationSource($"test_{timestamp}_simulation.json"));
            hf.Start();
        }

        public void SimulateEnd()
        {
            hf.Stop();
        }
       
    }

    [CollectionDefinition("Hoverfly Simulation")]
    public class DatabaseCollection : ICollectionFixture<HoverflyFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
