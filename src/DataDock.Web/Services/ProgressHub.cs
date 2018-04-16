using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Datadock.Common.Models;
using Microsoft.AspNetCore.SignalR;

namespace DataDock.Web.Services
{
    public class ProgressHub : Hub
    {
        public async Task ProgressUpdated(string userId, string jobId, string progressMessage)
        {
            // TODO: Change this to send to the specific user
            await Clients.All.SendAsync("progressUpdated", userId, jobId, progressMessage);
        }

        public async Task StatusUpdated(string userId, string jobId, JobStatus jobStatus)
        {
            // TODO: Change this to send to the specific user
            await Clients.All.SendAsync("statusUpdated", userId, jobId, jobStatus);
        }

    }
}
