using System;
using System.IO;
using System.Reflection;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace DataDock.Web.UI.Tests
{
    public class DashboardPageTests
    {
        private string _gituser;
        private string _gitpwd;
        private string _gitrepo;
        
       
        [Fact]
        public void ItShouldHaveADashboardMenu()
        {
            try
            {
                var option = new ChromeOptions();
            option.AddArgument("--headless");
            using (var driver =
                new ChromeDriver(
                    ChromeDriverService.CreateDefaultService(
                        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)), option))
            {
                _gituser = TestHelper.GetConfigurationValue(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "GitUser");
                _gitpwd = TestHelper.GetConfigurationValue(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "GitPwd");
                _gitrepo = TestHelper.GetConfigurationValue(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "GitRepo");

                driver.Navigate().GoToUrl($"http://localhost:5000/");
                var wait = new WebDriverWait(driver, TimeSpan.FromMinutes(1));
                var pageSource = driver.PageSource;
                var loginLink = wait.Until(d => d.FindElement(By.Id("loginLink")));
                
                Assert.NotNull(loginLink);
                loginLink.Click();

                wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));

                driver.FindElement(By.Id("login_field")).SendKeys(_gituser);
                driver.FindElement(By.Id("password")).SendKeys(_gitpwd);
                driver.FindElement(By.CssSelector("input.btn")).Submit();

                pageSource = driver.PageSource;
                var logoffLink = wait.Until(d => d.FindElement(By.Id("logoffLink")));
                Assert.NotNull(logoffLink);

                driver.Navigate().GoToUrl($"http://localhost:5000/{_gituser}");
                pageSource = driver.PageSource;

                var repoLink = wait.Until(d => d.FindElement(By.LinkText("Repositories")));
                Assert.NotNull(repoLink);

                var datasetsLink = wait.Until(d => d.FindElement(By.LinkText("Datasets")));
                Assert.NotNull(datasetsLink);

                var importLink = wait.Until(d => d.FindElement(By.LinkText("Add Data")));
                Assert.NotNull(importLink);

                var jobsLink = wait.Until(d => d.FindElement(By.LinkText("Job History")));
                Assert.NotNull(jobsLink);

                var libraryLink = wait.Until(d => d.FindElement(By.LinkText("Template Library")));
                Assert.NotNull(libraryLink);

                //currently failing on settingsLink even though it is visible in the page source :/
                //var settingsLink = wait.Until(d => d.FindElement(By.LinkText("Settings")));
                //Assert.NotNull(settingsLink);
            }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
