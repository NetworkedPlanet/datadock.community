using Datadock.Common.Models;
using Nest;
using System.Collections.Generic;

namespace Datadock.Common.Elasticsearch
{
    public static class QueryHelper
    {
        public static QueryContainer QueryByOwnerId(QueryContainerDescriptor<RepoSettings> q, string ownerId)
        {
            var mustClauses = new List<QueryContainer>
            {
                new TermQuery
                {
                    Field = new Field("ownerId"),
                    Value = ownerId
                }
            };
            return new BoolQuery { Must = mustClauses };
        }

        public static QueryContainer QueryByOwnerIdAndRepositoryId(QueryContainerDescriptor<RepoSettings> q, string ownerId, string repoId)
        {
            var filterClauses = new List<QueryContainer>
            {
                new TermQuery
                {
                    Field = new Field("ownerId"),
                    Value = ownerId
                },
                new TermQuery
                {
                    Field = new Field("repoId"),
                    Value = repoId
                }
            };
            return new BoolQuery { Filter = filterClauses };
        }

    }
}
