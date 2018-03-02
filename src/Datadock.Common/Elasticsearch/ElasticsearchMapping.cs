using Datadock.Common.Models;
using Nest;

namespace Datadock.Common.Elasticsearch
{
    public static class ElasticsearchMapping
    {
        public static IPromise<IMappings> UserAccountIndexMappings(MappingsDescriptor ms)
        {
            return ms.Map<UserAccount>(m => m.AutoMap(-1));
        }

        public static IPromise<IMappings> UserSettingsIndexMappings(MappingsDescriptor ms)
        {
            return ms.Map<UserSettings>(m => m.AutoMap(-1));
        }

        public static IPromise<IMappings> JobsIndexMappings(MappingsDescriptor ms)
        {
            return ms.Map<JobInfo>(m => m.AutoMap(-1));
        }
    }
}
