using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Datadock.Common.Models;
using Datadock.Common.Stores;
using DataDock.Web.Auth;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace DataDock.Web.Controllers
{
    [Authorize(Policy = "Admin")]
    [ServiceFilter(typeof(AccountExistsFilter))]
    public class ManageController : Controller
    {
        private readonly IDatasetStore _datasetStore;
        private readonly ISchemaStore _schemaStore;
        private readonly IRepoSettingsStore _repoSettingsStore;
        private readonly IOwnerSettingsStore _ownerSettingsStore;

        public ManageController(IDatasetStore datasetStore, ISchemaStore schemaStore)
        {
            _datasetStore = datasetStore;
            _schemaStore = schemaStore;
        }

        public IActionResult Index()
        {
            var model = new BaseLayoutViewModel {Title = "DataDock Admin"};
            model.Heading = model.Title;
            return View(model);
        }

        public async Task<IActionResult> SeedDatasets()
        {
            try
            {
                // create a few dummy datasets
                var datasets = GetDummyDatasets(1);
                foreach (var ds in datasets)
                {
                    await _datasetStore.CreateOrUpdateDatasetRecordAsync(ds);
                }

                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task<IActionResult> SeedTemplates()
        {
            try
            {
                // create a few dummy datasets
                var templates = GetDummyTemplates(1);
                foreach (var t in templates)
                {
                    await _schemaStore.CreateOrUpdateSchemaRecordAsync(t);
                }

                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private List<DatasetInfo> GetDummyDatasets(int num)
        {
            var datasets = new List<DatasetInfo>();

            var csvwJson = new JObject(new JProperty("dc:title", "Test Dataset"), new JProperty("dcat:keyword", new JArray("one", "two", "three")));
            var voidJson = new JObject(
                new JProperty("void:triples", "100"),
                new JProperty("void:dataDump", new JArray("https://github.com/jennet/animated-barnacle/releases/download/acsv_csv_20180207_170200/acsv_csv_20180207_170200.nt.gz", "http://datadock.io/jennet/animated-barnacle/csv/acsv.csv/acsv.csv")));
            var ownerId = User.Identity.Name;
            var datasetInfo = new DatasetInfo
            {
                OwnerId = ownerId,
                RepositoryId = "repo-name",
                DatasetId = "test.csv",
                ShowOnHomePage = true,
                LastModified = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1)),
                CsvwMetadata = csvwJson,
                VoidMetadata = voidJson
            };

            datasets.Add(datasetInfo);
            return datasets;
        }

        private List<SchemaInfo> GetDummyTemplates(int num)
        {
            var schemas = new List<SchemaInfo>();

            var csvwJson = new JObject(new JProperty("dc:title", "Test Dataset"), new JProperty("dcat:keyword", new JArray("one", "two", "three")));
            var ownerId = User.Identity.Name;
            var schemaInfo = new SchemaInfo()
            {
                OwnerId = ownerId,
                RepositoryId = "repo-name",
                SchemaId = Guid.NewGuid().ToString(),
                LastModified = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1)),
                Schema = csvwJson
            };

            schemas.Add(schemaInfo);
            return schemas;
        }
    }
}