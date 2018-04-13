using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using Datadock.Common.Stores;
using DataDock.Web.Filters;
using DataDock.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Serilog;

namespace DataDock.Web.Api
{
    [Produces("application/json")]
    [Route("api/data")]
    public class DataController : Controller
    {
        private readonly IUserStore _userStore;
        private readonly IJobStore _jobStore;
        private readonly IImportFormParser _parser;

        public DataController(IUserStore userStore, 
            IRepoSettingsStore repoSettingsStore,
            IJobStore jobStore,
            IImportFormParser parser)
        {
            _userStore = userStore;
            _jobStore = jobStore;
            _parser = parser;
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

                var parserResult = await _parser.ParseImportFormAsync(Request, userId,
                    async (formData, formCollection) =>
                    {
                        var formValueProvider = new FormValueProvider(
                            BindingSource.Form,
                            formCollection,
                            CultureInfo.CurrentCulture);
                        return await TryUpdateModelAsync(formData, "", formValueProvider);
                    });
                if (!parserResult.IsValid)
                {
                    if (!ModelState.IsValid) return BadRequest(ModelState);
                    return BadRequest(parserResult.ValidationErrors);
                }
                
                var job = await _jobStore.SubmitImportJobAsync(parserResult.ImportJobRequest);
                
                Log.Information("api/data(POST): Conversion job started.");

                if (parserResult.SchemaImportJobRequest != null)
                {
                    try
                    {
                        await _jobStore.SubmitSchemaImportJobAsync(parserResult.SchemaImportJobRequest);

                        Log.Information("api/data(POST): Schema creation job started.");
                    }
                    catch (Exception)
                    {
                        Log.Error("api/data(POST): Unexpected error staring schema creation job.");
                    }
                }

                return Ok(new DataControllerResult { Message = "API called successfully", Metadata = parserResult.Metadata, JobId = job.JobId});
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Fatal error in api/data '{ex.Message}'");
                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }
        }
    }

    /// <summary>
    /// Structure returned from the Post method of the DataController
    /// </summary>
    public class DataControllerResult
    {
        public string Message { get; set; }
        public string Metadata { get; set; }
        public string JobId { get; set; }
    }

}