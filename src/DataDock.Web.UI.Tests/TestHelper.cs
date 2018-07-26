using Microsoft.Extensions.Configuration;

namespace DataDock.Web.UI.Tests
{
    public static class TestHelper
    {
        public static IConfiguration GetIConfigurationRoot(string outputPath)
        {
            return new ConfigurationBuilder()
                .SetBasePath(outputPath)
                .AddJsonFile("appsettings.json", optional: true)
               // .AddEnvironmentVariables()
                .Build();
        }

        public static string GetConfigurationValue(string outputPath, string variableName)
        {
            var iConfig = GetIConfigurationRoot(outputPath);
            var value = iConfig[variableName];
            return value;
        }
    }

}
