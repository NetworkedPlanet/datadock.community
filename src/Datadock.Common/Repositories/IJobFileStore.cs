﻿using System.IO;
using System.Threading.Tasks;

namespace Datadock.Common.Repositories
{
    public interface IFileStore
    {
        Task<string> AddFileAsync(Stream file);

        Task<Stream> GetFileAsync(string fileId);

        Task DeleteFileAsync(string fileId);
    }
}
