using System;
using Datadock.Common.Models;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace DataDock.Web.Services
{
    public class ProgressHub : Hub
    {
        public async Task ProgressUpdated(string userId, string jobId, string progressMessage)
        {
            // TODO: Change this to send to the specific user
            await Clients.Group(userId).SendAsync("progressUpdated", userId, jobId, progressMessage);
            //await Clients.All.SendAsync("progressUpdated", userId, jobId, progressMessage);
        }

        public async Task StatusUpdated(string userId, string jobId, JobStatus jobStatus)
        {
            // TODO: Change this to send to the specific user
            // await Clients.All.SendAsync("statusUpdated", userId, jobId, jobStatus);
            await Clients.Group(userId).SendAsync("statusUpdated", userId, jobId, jobStatus);
        }

        public async Task SendMessage(string userId, string message)
        {
            //await Clients.All.SendAsync("sendMessage", userId, message);
            await Clients.Group(userId).SendAsync("sendMessage", userId, message);
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                var name = Context.User.Identity.Name;
                await Groups.AddAsync(Context.ConnectionId, name);
            }
            catch (Exception)
            {
                // A connection from the worker role will not have a user identity
            }

            await base.OnConnectedAsync();
        }
    }
}
