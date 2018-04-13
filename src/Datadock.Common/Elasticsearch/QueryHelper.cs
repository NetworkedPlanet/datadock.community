﻿using Nest;
using System.Collections.Generic;

namespace Datadock.Common.Elasticsearch
{
    public static class QueryHelper
    {
        public static QueryContainer QueryByOwnerId(string ownerId)
        {
            var filterClauses = new List<QueryContainer>
            {
                new TermQuery
                {
                    Field = new Field("ownerId"),
                    Value = ownerId
                }
            };
            return new BoolQuery { Filter = filterClauses };
        }


        public static QueryContainer QueryByOwnerIds(string[] ownerIds)
        {
            var filterClauses = new List<QueryContainer>
            {
                new TermQuery
                {
                    Field = new Field("ownerId"),
                    Value = ownerIds
                }
            };
            return new BoolQuery { Filter = filterClauses };
        }
        public static QueryContainer QueryByOwnerIdAndRepositoryId(string ownerId, string repositoryId)
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
                    Field = new Field("repositoryId"),
                    Value = repositoryId
                }
            };
            return new BoolQuery { Filter = filterClauses };
        }
        public static QueryContainer QueryByOwnerIdAndRepositoryIds(string ownerId, string[] repositoryIds)
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
                    Field = new Field("repositoryId"),
                    Value = repositoryIds
                }
            };
            return new BoolQuery { Filter = filterClauses };
        }
    }
}
