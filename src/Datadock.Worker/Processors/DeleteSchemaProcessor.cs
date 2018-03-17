using System;
using System.Threading.Tasks;
using Datadock.Common.Models;
using Datadock.Common.Repositories;
using Serilog;

namespace DataDock.Worker.Processors
{
    public class DeleteSchemaProcessor : IDataDockProcessor
    {
        private readonly ISchemaRepository _schemaRepository;
        private readonly IProgressLog _progressLog;

        public DeleteSchemaProcessor(ISchemaRepository schemaRepository, IProgressLog progressLog)
        {
            _schemaRepository = schemaRepository;
            _progressLog = progressLog;
        }

        public async Task ProcessJob(JobInfo job, UserAccount userAccount)
        {
            // Delete the schema from documentDB
            try
            {
                _progressLog.UpdateStatus(JobStatus.Running, $"Deleting schema {job.SchemaId}");
                await _schemaRepository.DeleteSchemaAsync(null, job.SchemaId);
                _progressLog.UpdateStatus(JobStatus.Running, "Schema deleted successfully");
            }
            catch (Exception ex)
            {
                _progressLog.Error("Failed to remove schema record");
                Log.Error(ex, "Failed to remove schema record");
                throw new WorkerException(ex, "Failed to delete schema record.");
            }
        }
    }
}
