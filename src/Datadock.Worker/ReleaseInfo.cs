using System.Collections.Generic;

namespace DataDock.Worker
{
    public class ReleaseInfo
    {
        public string Tag { get; }
        public List<string> DownloadLinks { get; }

        public ReleaseInfo(string tag)
        {
            Tag = tag;
            DownloadLinks = new List<string>();
        }
    }
}
