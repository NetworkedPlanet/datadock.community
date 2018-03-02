using System;
using System.Collections.Generic;
using System.Text;
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
    }
}
