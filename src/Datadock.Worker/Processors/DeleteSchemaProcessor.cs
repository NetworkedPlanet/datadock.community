using System;
using System.Threading.Tasks;
using Datadock.Common.Models;
using Datadock.Common.Stores;
using Serilog;

namespace DataDock.Worker.Processors
{
    public class DeleteSchemaProcessor : IDataDockProcessor
    {
        private readonly ISchemaRepository _schemaRepository;

        public DeleteSchemaProcessor(ISchemaRepository schemaRepository)
        {
            _schemaRepository = schemaRepository;
        }

        public async Task ProcessJob(JobInfo job, UserAccount userAccount, IProgressLog progressLog)
        {
            // Delete the schema from documentDB
            try
            {
                progressLog.UpdateStatus(JobStatus.Running, $"Deleting schema {job.SchemaId}");
                await _schemaRepository.DeleteSchemaAsync(null, job.SchemaId);
                progressLog.UpdateStatus(JobStatus.Running, "Schema deleted successfully");
            }
            catch (Exception ex)
            {
                progressLog.Error("Failed to remove schema record");
                Log.Error(ex, "Failed to remove schema record");
                throw new WorkerException(ex, "Failed to delete schema record.");
            }
        }
    }
}
