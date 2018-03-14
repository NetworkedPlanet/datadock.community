using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DataDock.Worker
{
    public interface IResourceFileMapper
    {
        string GetPathFor(Uri resourceUri);
        bool CanMap(Uri resourceUri);
    }

    public class ResourceFileMapper : IResourceFileMapper
    {
        private readonly List<ResourceMapEntry> _mapEntries;
        public ResourceFileMapper(List<ResourceMapEntry> entries)
        {
            _mapEntries = entries;
        }

        public string GetPathFor(Uri resourceUri)
        {
            if (resourceUri == null) return null;
            foreach (var entry in _mapEntries)
            {
                if (entry.BaseUri.IsBaseOf(resourceUri))
                {
                    var subjectRel = entry.BaseUri.MakeRelativeUri(resourceUri).ToString();
                    subjectRel = Uri.UnescapeDataString(subjectRel);
                    var subjectPath = Path.Combine(subjectRel.Split('/'));
                    if (string.Empty.Equals(subjectRel) || subjectRel.EndsWith("/"))
                    {
                        subjectPath = Path.Combine(subjectPath, "index");
                    }
                    return Path.Combine(entry.OutputDirectoryPath, subjectPath);
                }
            }
            return null;
        }

        public bool CanMap(Uri resourceUri)
        {
            return resourceUri != null && _mapEntries.Any(m => m.BaseUri.IsBaseOf(resourceUri));
        }
    }

    public class ResourceMapEntry
    {
        public Uri BaseUri { get; }
        public string OutputDirectoryPath { get; }

        public ResourceMapEntry(Uri baseUri, string outputDirectoryPath)
        {
            BaseUri = baseUri;
            OutputDirectoryPath = outputDirectoryPath;
        }
    }

}
