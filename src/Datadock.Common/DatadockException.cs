using System;

namespace Datadock.Common
{
    /// <summary>
    /// Common base-class for DD-specific exceptions
    /// </summary>
    public class DatadockException: Exception
    {
        public DatadockException(string msg) : base(msg) { }
    }
}
