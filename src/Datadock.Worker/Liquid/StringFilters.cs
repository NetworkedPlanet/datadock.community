using System;

namespace Datadock.Worker.Liquid
{
    public static class StringFilters
    {
        public static string UnescapeDataString(string input)
        {
            return Uri.UnescapeDataString(input);
        }
    }
}
