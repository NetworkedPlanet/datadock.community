using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Datadock.Common.Models;
using Datadock.Common.Stores;
using DataDock.Web.Filters;
using DataDock.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace DataDock.Web.Api
{
    [Produces("application/json")]
    [Route("api/data")]
    public class DataController : Controller
    {
        private static readonly FormOptions _defaultFormOptions = new FormOptions();
        private readonly IUserStore _userStore;
        private readonly IRepoSettingsStore _repoSettingsStore;
        private readonly IFileStore _fileStore;
        private readonly IJobStore _jobStore;

        public DataController(IUserStore userStore, 
            IRepoSettingsStore repoSettingsStore,
            IFileStore fileStore,
            IJobStore jobStore)
        {
            _userStore = userStore;
            _repoSettingsStore = repoSettingsStore;
            _fileStore = fileStore;
            _jobStore = jobStore;
        }

        // GET: api/Data
        [HttpGet]
        public HttpResponseMessage Get()
        {
            return new HttpResponseMessage()
            {
                Content = new StringContent("DataDock API")
            };
        }

        [HttpPost]
        [DisableFormValueModelBinding]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Post()
        {
            try
            {
                if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
                {
                    return BadRequest($"Expected a multipart request, but got {Request.ContentType}");
                }

                // Validate user name and authentication status
                var userId = User?.Identity?.Name;
                if (string.IsNullOrEmpty(userId))
                {
                    Log.Debug("Import: No value found for user principal name");
                    return Unauthorized();
                }

                if (User?.Identity?.IsAuthenticated != true)
                {
                    Log.Debug("Import: User identity is not authenticated");
                    return Unauthorized();
                }

                // Retrieve settings
                var userSettings = await _userStore.GetUserSettingsAsync(userId);
                if (userSettings == null)
                {
                    Log.Debug("Import: User settings not found.");
                    return Unauthorized();
                }

                var jobInfo = new ImportJobRequestInfo
                {
                    UserId = userId
                };

                var formAccumulator = new KeyValueAccumulator();
                string targetFilePath = null;

                var boundary = MultipartRequestHelper.GetBoundary(
                    MediaTypeHeaderValue.Parse(Request.ContentType),
                    _defaultFormOptions.MultipartBoundaryLengthLimit);
                var reader = new MultipartReader(boundary, HttpContext.Request.Body);
                var section = await reader.ReadNextSectionAsync();
                while (section != null)
                {
                    ContentDispositionHeaderValue contentDisposition;
                    var hasContentDispositionHeader =
                        ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out contentDisposition);
                    if (hasContentDispositionHeader)
                    {
                        // TODO - if no file upload then badrequest
                        if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition))
                        {

                            // todo log file name
                            jobInfo.CsvFileName = string.Empty;
                            jobInfo.DatasetId = string.Empty;
                            Log.Information("api/data(POST): Starting conversion job. UserId='{0}', File='{1}'", userId, "");
                            var csvFileId = await _fileStore.AddFileAsync(section.Body);

                            Log.Information($"Saved the uploaded CSV file '{csvFileId}'");
                            jobInfo.CsvFileId = csvFileId;
                        }
                        else if (MultipartRequestHelper.HasFormDataContentDisposition(contentDisposition))
                        {
                            var key = HeaderUtilities.RemoveQuotes(contentDisposition.Name);
                            var encoding = GetEncoding(section);
                            using (var streamReader = new StreamReader(
                                section.Body,
                                encoding,
                                detectEncodingFromByteOrderMarks: true,
                                bufferSize: 1024,
                                leaveOpen: true))
                            {
                                // The value length limit is enforced by MultipartBodyLengthLimit
                                var value = await streamReader.ReadToEndAsync();
                                if (String.Equals(value, "undefined", StringComparison.OrdinalIgnoreCase))
                                {
                                    value = String.Empty;
                                }
                                formAccumulator.Append(key.ToString(), value);

                                if (formAccumulator.ValueCount > _defaultFormOptions.ValueCountLimit)
                                {
                                    throw new InvalidDataException($"Form key count limit {_defaultFormOptions.ValueCountLimit} exceeded.");
                                }
                            }
                        }
                    }
                    // Drains any remaining section body that has not been consumed and
                    // reads the headers for the next section.
                    section = await reader.ReadNextSectionAsync();
                }

                // Bind form data to a model
                var formData = new ImportFormData();
                var formValueProvider = new FormValueProvider(
                    BindingSource.Form,
                    new FormCollection(formAccumulator.GetResults()),
                    CultureInfo.CurrentCulture);
                var bindingSuccessful = await TryUpdateModelAsync(formData, prefix: "",
                    valueProvider: formValueProvider);
                if (!bindingSuccessful)
                {
                    if (!ModelState.IsValid)
                    {
                        return BadRequest(ModelState);
                    }
                }

                jobInfo.OwnerId = formData.OwnerId;
                jobInfo.RepositoryId = formData.RepoId;
                if (string.IsNullOrEmpty(formData.OwnerId) || string.IsNullOrEmpty(formData.RepoId))
                {
                    Log.Error("DataController: POST called with no owner or repo set in FormData");
                    return BadRequest("No target repository supplied");
                }

                if (formData.Metadata == null)
                {
                    Log.Error("DataController: POST called with no metadata present in FormData");
                    return BadRequest("No metadata supplied");
                }
                var parser = new JsonSerializer();
                Log.Debug("DataController: Metadata: {0}", formData.Metadata);
                var metadataObject = parser.Deserialize(new JsonTextReader(new StringReader(formData.Metadata))) as JObject;
                if (metadataObject == null)
                {
                    Log.Error(
                        "DataController: Error deserializing metadata as object, unable to create conversion job. Metadata = '{0}'",
                        formData.Metadata);
                    return BadRequest("Metadata badly formatted");
                }

                var datasetIri = metadataObject["url"]?.ToString();
                var datasetTitle = metadataObject["dc:title"]?.ToString();
                if (string.IsNullOrEmpty(datasetIri))
                {
                    Log.Error("DataController: No dataset IRI supplied in metadata.");
                    return BadRequest("No dataset IRI supplied in metadata");
                }
                Log.Debug("DataController: datasetIri = '{0}'", datasetIri);
                jobInfo.DatasetIri = datasetIri;

                // save CSVW to file storage
                if (string.IsNullOrEmpty(jobInfo.CsvFileName))
                {
                    jobInfo.CsvFileName = formData.Filename;
                    jobInfo.DatasetId = formData.Filename;
                }
                
                byte[] byteArray = Encoding.UTF8.GetBytes(formData.Metadata);
                var metadataStream = new MemoryStream(byteArray);
                var csvwFileId = await _fileStore.AddFileAsync(metadataStream);
                jobInfo.CsvmFileId = csvwFileId;
                

                var job = await _jobStore.SubmitImportJobAsync(jobInfo);
                
                Log.Information("api/data(POST): Conversion job started.");

                if (formData.SaveAsSchema)
                {
                    try
                    {
                        Log.Information("api/data(POST): Saving metadata as template.");

                        var schema = new JObject(new JProperty("dc:title", "Template from " + datasetTitle), new JProperty("metadata", metadataObject));
                        Log.Information("api/data(POST): Starting schema creation job. UserId='{0}', Repository='{1}'", userId, formData.RepoId);

                        byte[] schemaByteArray = Encoding.UTF8.GetBytes(schema.ToString());
                        var schemaStream = new MemoryStream(schemaByteArray);
                        var schemaFileId = await _fileStore.AddFileAsync(schemaStream);

                        if (!string.IsNullOrEmpty(schemaFileId))
                        {
                            Log.Information("api/data(POST): Schema temp file saved: {0}.", schemaFileId);
                            var schemaJobRequest = new SchemaImportJobRequestInfo()
                            {
                                UserId = userId,
                                SchemaFileId = schemaFileId,
                                OwnerId = formData.OwnerId,
                                RepositoryId = formData.RepoId
                            };
                            var sj = await _jobStore.SubmitSchemaImportJobAsync(schemaJobRequest);
                            
                            Log.Information("api/data(POST): Schema creation job started.");
                        }
                        else
                        {
                            Log.Error("api/data(POST): Error saving schema content to temporary file storage, unable to start schema creation job");
                        }

                    }
                    catch (Exception e)
                    {
                        Log.Error("api/data(POST): Unexpected error staring schema creation job.");
                    }
                }

                return Ok(new DataControllerResult { Message = "API called successfully: " + job.JobId, Metadata = formData.Metadata });
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Fatal error in api/data '{ex.Message}'");
                return StatusCode(500);
            }
        }

        private static Encoding GetEncoding(MultipartSection section)
        {
            MediaTypeHeaderValue mediaType;
            var hasMediaTypeHeader = MediaTypeHeaderValue.TryParse(section.ContentType, out mediaType);
            // UTF-7 is insecure and should not be honored. UTF-8 will succeed in 
            // most cases.
            if (!hasMediaTypeHeader || Encoding.UTF7.Equals(mediaType.Encoding))
            {
                return Encoding.UTF8;
            }
            return mediaType.Encoding;
        }

        private static bool GetBool(NameValueCollection formData, string parameterName, bool defaultValue)
        {
            Log.Debug("DataController: GetBool called for FormData parameter '{0}' with default value '{1}'", parameterName, defaultValue);
            var ret = defaultValue;
            var strValue = formData[parameterName];
            Log.Debug("DataController: origin value of '{0}' in FormData is '{1}'", strValue);
            if (!string.IsNullOrEmpty(strValue))
            {
                try
                {
                    ret = Convert.ToBoolean(strValue);
                }
                catch (Exception ex)
                {
                    Log.Error(ex,
                        $"Error getting {parameterName} boolean from form data. Received string value {strValue}");
                }
            }
            Log.Debug("DataController: GetBool returning {0}", ret);
            return ret;
        }

    }

    /// <summary>
    /// Structure returned from the Post method of the DataController
    /// </summary>
    public class DataControllerResult
    {
        public string Message { get; set; }
        public string Metadata { get; set; }
    }
}