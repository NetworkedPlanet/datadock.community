using System;
using System.IO;
using System.Reflection;
using Hoverfly.Core;
using Hoverfly.Core.Configuration;
using Hoverfly.Core.Resources;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace DataDock.Web.UI.Tests
{
    public class HoverflyDashboardPageTests : IClassFixture<HoverflyFixture>
    {
        private readonly ChromeOptions _options;

        public HoverflyDashboardPageTests(HoverflyFixture hf)
        {
            _options = new ChromeOptions();
            _options.AddArgument("--headlesss");
            _options.Proxy = new Proxy {HttpProxy = "localhost:" + hf.ProxyPort, SslProxy = "localhost:" + hf.ProxyPort};
            _options.AcceptInsecureCertificates = true;
            _options.Proxy.AddBypassAddress("localhost");
        }
        [Fact]
        public void GitHubIsAwesome()
        {
            using (var driver = new ChromeDriver(ChromeDriverService.CreateDefaultService(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)), _options))
            {
                driver.Navigate().GoToUrl("https://github.com/");
                var wait = new WebDriverWait(driver, TimeSpan.FromMinutes(1));
                wait.Until(d => d.FindElement(By.ClassName("title")).Displayed);
                Assert.Equal("Awesome", driver.FindElement(By.ClassName("title")).Text);
            }
        }
    }
}
