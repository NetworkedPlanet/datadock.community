using System;
using System.IO;
using System.Reflection;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace DataDock.Web.UI.Tests
{
    public class HomePageTests
    {
        [Fact]
        public void ItShouldHaveAClickableAboutPageLink()
        {
            var option = new ChromeOptions();
            option.AddArgument("--headless");
            using (var driver = new ChromeDriver(ChromeDriverService.CreateDefaultService(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)), option))
            {
                driver.Navigate().GoToUrl("http://localhost:5000/");
                var wait = new WebDriverWait(driver, TimeSpan.FromMinutes(1));
                var clickableElement = wait.Until(d=>d.FindElement(By.LinkText("About")));
                clickableElement.Click();
            }
        }
    }
}
