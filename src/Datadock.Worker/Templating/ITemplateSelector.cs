﻿using System;
using System.Collections.Generic;
using VDS.RDF;

namespace Datadock.Worker.Templating
{
    public interface ITemplateSelector
    {
        string SelectTemplate(Uri subjectIri, IList<Triple> triples);
    }
}
