using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Datadock.Common.Models;

namespace DataDock.Worker
{
    interface IDataDockProcessor
    {
        Task ProcessJob(JobInfo jobInfo, UserAccount userInfo);
    }
}
