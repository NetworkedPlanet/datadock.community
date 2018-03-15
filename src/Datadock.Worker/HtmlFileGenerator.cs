﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DataDock.Common;
using NetworkedPlanet.Quince;
using VDS.RDF;

namespace DataDock.Worker
{
    public class HtmlFileGenerator : IResourceStatementHandler
    {
        private readonly IResourceFileMapper _resourceMap;
        private readonly IViewEngine _viewEngine;
        private readonly IProgressLog _progressLog;
        private int _numFilesGenerated;
        private readonly Regex _idRegex;
        private readonly int _reportInterval;

        public HtmlFileGenerator(IResourceFileMapper resourceMap, IViewEngine viewEngine, IProgressLog progressLog, int reportInterval)
        {
            _resourceMap = resourceMap;
            _viewEngine = viewEngine;
            _progressLog = progressLog;
            _numFilesGenerated = 0;
            _idRegex = DataDockUrlHelper.IdentifierRegex;
            _reportInterval = reportInterval;
        }


        public bool HandleResource(INode resourceNode, IList<Triple> subjectStatements, IList<Triple> objectStatements)
        {
            if (subjectStatements == null || subjectStatements.Count == 0) return true;
            var subject = (resourceNode as IUriNode)?.Uri;
            var nquads = subject == null ? null : _idRegex.Replace(subject.ToString(), DataDockUrlHelper.PublishSite + "$1/$2/data/$3.nq");
            try
            {
                var targetPath = _resourceMap.GetPathFor(subject);
                if (targetPath != null)
                {
                    targetPath += ".html";
                    var targetDir = Path.GetDirectoryName(targetPath);
                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }
                    var html = _viewEngine.Render(subject, subjectStatements, objectStatements,
                        new Dictionary<string, object> {{"nquads", nquads}});
                    using (var stream = File.Open(targetPath, FileMode.Create, FileAccess.ReadWrite))
                    {
                        using (var writer = new StreamWriter(stream, Encoding.UTF8))
                        {
                            writer.Write(html);
                        }
                        stream.Close();
                    }
                }
                _numFilesGenerated++;
                if (_numFilesGenerated % _reportInterval == 0)
                {
                    _progressLog.Info("Generating static HTML files - {0} files created/updated.", _numFilesGenerated);
                }
            }
            catch (Exception ex)
            {
                _progressLog.Exception(ex, "Error generating HTML file for subject {0}: {1}", subject, ex.Message);
            }
            return true;
        }
    }
}