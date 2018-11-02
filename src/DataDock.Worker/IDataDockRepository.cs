using System;
using System.Collections.Generic;
using DataDock.Common.Models;
using DataDock.Worker.Liquid;
using VDS.RDF;

namespace DataDock.Worker
{
    public interface IDataDockRepository
    {
        void DeleteDataset(Uri datasetIri);
        void GenerateRdf(IEnumerable<Uri> graphFilter);
        void Publish(IEnumerable<Uri> graphFilter = null, PortalInfoDrop portalInfo = null);
        void UpdateDataset(IGraph insertTriples, Uri datasetIri, bool dropExistingGraph, IGraph metadataGraph, Uri metadataGraphIri, IGraph definitionsGraph, Uri definitionsGraphIri, Uri publisherIri, ContactInfo publisherInfo, string repositoryTitle, string repositoryDescription, Uri rootMetadataGraphIri);
    }
}