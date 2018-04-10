using Datadock.Common.Models;
using DataDock.Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace DataDock.Web.ViewModels
{
    public class DatasetViewModel
    {
        private readonly DatasetInfo _datasetInfo;
        private readonly JObject _csvwMetadata;
        private readonly JObject _voidMetadata;

        public DatasetViewModel(DatasetInfo datasetInfo)
        {
            _datasetInfo = datasetInfo;
            _csvwMetadata = datasetInfo.CsvwMetadata as JObject;
            _voidMetadata = datasetInfo.VoidMetadata as JObject;

            Title = this.GetTitle();
            Description = this.GetDescription();
        }

        [Display(Name = "Identifier")]
        public string Id => _datasetInfo.DatasetId;
        public string RepositoryId => _datasetInfo.RepositoryId;
        public string OwnerId => _datasetInfo.OwnerId;

        public string Iri => this.GetIri();

        [Display(Name = "Title")]
        public string Title { get; set; }
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Tags")]
        public IEnumerable<string> Tags => this.GetTags();

        [Display(Name = "License")]
        public string LicenseUri => this.GetLicenseUri();
        public string LicenseIcon => this.GetLicenseIcon();

        [Display(Name = "Last Modified")]
        public DateTime LastModified => _datasetInfo.LastModified;

        public bool? ShowOnHomePage => _datasetInfo.ShowOnHomePage;

        public string GetIri()
        {
            if (_csvwMetadata != null)
            {
                return _csvwMetadata["url"].ToString();
            }
            return DataDockUrlHelper.GetDatasetIdentifier(_datasetInfo.RepositoryId, _datasetInfo.DatasetId);
        }

        public string GetTitle()
        {
            if (_csvwMetadata != null)
            {
                return GetLiteralValue(_csvwMetadata, "dc:title");
            }
            return _datasetInfo.RepositoryId + "/" + _datasetInfo.DatasetId;
        }

        public string GetDescription()
        {
            if (_csvwMetadata != null)
            {
                return GetLiteralValue(_csvwMetadata, "dc:description");
            }
            return string.Empty;
        }

        public string GetLicenseUri()
        {
            if (_csvwMetadata != null)
            {
                return GetLiteralValue(_csvwMetadata, "dc:license");
            }
            return string.Empty;
        }

        public string GetLicenseIcon()
        {
            switch (GetLicenseUri())
            {
                case "https://creativecommons.org/publicdomain/zero/1.0/":
                    return "cc-zero.png";
                case "https://creativecommons.org/licenses/by/4.0/":
                    return "cc-by.png";
                case "https://creativecommons.org/licenses/by-sa/4.0/":
                    return "cc-by-sa.png";
                case "http://www.nationalarchives.gov.uk/doc/open-government-licence/version/3/":
                    return "ogl.png";
                case "https://opendatacommons.org/licenses/pddl/":
                    return "PDDL.png";
                case "https://opendatacommons.org/licenses/by/":
                    return "ODC-By.png";
            }
            return string.Empty;
        }

        public IEnumerable<string> GetTags()
        {
            var tags = _csvwMetadata?["dcat:keyword"] as JArray;
            if (tags != null)
            {
                return tags.Select(t => (t as JValue)?.Value<string>());
            }
            return new string[0];
        }


        private static string GetLiteralValue(JObject parentObject, string propertyName, string defaultValue = null)
        {
            var titles = parentObject[propertyName] as JArray;
            if (titles != null) return GetBestLanguageMatch(titles, null);
            var title = parentObject[propertyName] as JValue;
            if (title != null) return GetLiteralValue(title);
            return defaultValue;
        }

        private static string GetLiteralValue(JToken literalToken)
        {
            var litObj = literalToken as JObject;
            if (litObj != null) return (litObj["@value"] as JValue)?.Value<string>();
            return (literalToken as JValue)?.Value<string>();
        }

        private static string GetBestLanguageMatch(JArray literalArray, string prefLang)
        {
            throw new NotImplementedException();
        }
    }
}
