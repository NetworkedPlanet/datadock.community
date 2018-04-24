using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Datadock.Common.Elasticsearch;
using Datadock.Common.Models;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace DataDock.IntegrationTests
{
    public class SchemaStoreTests : IClassFixture<ElasticsearchFixture>
    {
        private readonly ElasticsearchFixture _fixture;
        private readonly SchemaStore _repo;

        public SchemaStoreTests(ElasticsearchFixture fixture)
        {
            _fixture = fixture;
            _repo = new SchemaStore(fixture.Client, fixture.Configuration);
        }

        [Fact]
        public async Task ItCanCreateAndRetrieveASimpleSchema()
        {
            var schemaInfo = new SchemaInfo
            {
                OwnerId = "the_owner",
                RepositoryId = "the_repo",
                LastModified = DateTime.UtcNow,
                SchemaId = "the_schema_id",
                Schema = @"
                {
                    foo : 'foo',
                    bar : {
                        baz : 'baz'
                    }
                }"
            };
            await _repo.CreateOrUpdateSchemaRecordAsync(schemaInfo);
            Thread.Sleep(1000);
            var retrievedSchema = await _repo.GetSchemaInfoAsync("the_owner", "the_schema_id");
            retrievedSchema.Id.Should().Be("the_owner/the_repo/the_schema_id");
            dynamic schema = JsonConvert.DeserializeObject<ExpandoObject>(retrievedSchema.Schema);
            //dynamic schema = JObject.Parse(retrievedSchema.Schema);
            Assert.Equal("foo", schema.foo);
            Assert.Equal("baz", schema.bar.baz);
            retrievedSchema.LastModified.Should().BeCloseTo(schemaInfo.LastModified);
        }

    }

    public class SchemaStoreFixture : ElasticsearchFixture
    {
        public SchemaStore Store { get; }
        public SchemaStoreFixture() : base()
        {
            Store = new SchemaStore(Client, Configuration);
            InitializeRepository().Wait();
            Thread.Sleep(1000);
        }

        private async Task InitializeRepository()
        {
            for (var o = 0; o < 5; o++)
            {
                for (var r = 0; r < 5; r++)
                {
                    var schemaInfo = new SchemaInfo
                    {
                        OwnerId = "owner-" + o,
                        RepositoryId = "repo-" + r,
                        SchemaId = "schema_" + o + "." + r,
                        LastModified = DateTime.UtcNow,
                        Schema = "{ foo : 'foo' }"
                    };
                    await Store.CreateOrUpdateSchemaRecordAsync(schemaInfo);
                }
            }

        }

    }

    public class SchemaStoreSearchTests : IClassFixture<SchemaStoreFixture>
    {
        private readonly SchemaStore _repo;

        public SchemaStoreSearchTests(SchemaStoreFixture fixture)
        {
            _repo= fixture.Store;
        }


        [Fact]
        public void ItCanRetrieveMultipleSchemasForASingleOwner()
        {
            var results = _repo.GetSchemasByOwnerList(new string[] {"owner-0"}, 0, 10);
            results.Count.Should().Be(5);
            foreach (var r in results)
            {
                r.OwnerId.Should().Be("owner-0");
                r.SchemaId.Should().StartWith("schema_0.");
            }
        }

        [Fact]
        public void ItCanRetrieveMultipleSchemasForMultipleOwners()
        {
            var results = _repo.GetSchemasByOwnerList(new[] {"owner-1", "owner-2"}, 0, 10);
            results.Count.Should().Be(10);
            foreach (var r in results)
            {
                r.OwnerId.Should().BeOneOf("owner-1", "owner-2");
            }
        }

        [Fact]
        public void ItCanRetrieveMultipleSchemasForMultipleOwnersWithSkip()
        {
            var results = _repo.GetSchemasByOwnerList(new[] { "owner-1", "owner-2" }, 5, 10);
            results.Count.Should().Be(5);
            foreach (var r in results)
            {
                r.OwnerId.Should().BeOneOf("owner-1", "owner-2");
            }
        }

        [Fact]
        public void ItCanRetrieveMultipleSchemasForMultipleOwnersWithSkipAndTake()
        {
            var results = _repo.GetSchemasByOwnerList(new[] { "owner-1", "owner-2" }, 5, 3);
            results.Count.Should().Be(3);
            foreach (var r in results)
            {
                r.OwnerId.Should().BeOneOf("owner-1", "owner-2");
            }
        }

        [Fact]
        public void ItCanRetrieveMultipleSchemasForMultipleRepositories()
        {
            var results =
                _repo.GetSchemasByRepositoryList("owner-1", new[] {"repo-0", "repo-1", "repo-2"}, 0, 10);
            results.Count.Should().Be(3);
            foreach (var r in results)
            {
                r.OwnerId.Should().Be("owner-1");
                r.RepositoryId.Should().BeOneOf("repo-0", "repo-1", "repo-2");
            }
        }

    }
}
