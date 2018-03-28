using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Datadock.Common.Elasticsearch;
using Datadock.Common.Models;
using FluentAssertions;
using Xunit;

namespace DataDock.IntegrationTests
{
    public class SchemaRepositoryTests : IClassFixture<ElasticsearchFixture>
    {
        private readonly ElasticsearchFixture _fixture;
        private readonly SchemaStore _repo;

        public SchemaRepositoryTests(ElasticsearchFixture fixture)
        {
            _fixture = fixture;
            _repo = new SchemaStore(fixture.Client, fixture.SchemasIndexName);
        }

        [Fact]
        public async Task ItCanCreateAndRetrieveASimpleSchema()
        {
            var schemaInfo = new SchemaInfo
            {
                Id = "simple_schema",
                OwnerId = "the_owner",
                RepositoryId = "the_repo",
                LastModified = DateTime.UtcNow,
                SchemaId = "the_schema_id",
                Schema = new 
                {
                    foo = "foo",
                    bar = new {
                        baz = "baz"
                    }
                }
            };
            await _repo.CreateOrUpdateSchemaRecordAsync(schemaInfo);
            Thread.Sleep(1000);
            var retrievedSchema = await _repo.GetSchemaInfoAsync("the_owner", "the_schema_id");
            retrievedSchema.Id.Should().Be("simple_schema");
            ((string)retrievedSchema.Schema.foo).Should().Be("foo");
            ((string) retrievedSchema.Schema.bar.baz).Should().Be("baz");
            retrievedSchema.LastModified.Should().BeCloseTo(schemaInfo.LastModified);
        }

    }

    public class SchemaRepositoryFixture : ElasticsearchFixture
    {
        public SchemaStore Store { get; }
        public SchemaRepositoryFixture() : base()
        {
            Store = new SchemaStore(Client, SchemasIndexName);
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
                        Id = Guid.NewGuid().ToString("N"),
                        OwnerId = "owner_" + o,
                        RepositoryId = "owner_" + o + "/repo_" + r,
                        SchemaId = "schema_" + o + "." + r,
                        LastModified = DateTime.UtcNow,
                        Schema = new { foo = "foo" }
                    };
                    await Store.CreateOrUpdateSchemaRecordAsync(schemaInfo);
                }
            }

        }

    }

    public class SchemaRepositorySearchTests : IClassFixture<SchemaRepositoryFixture>
    {
        private readonly SchemaStore _repo;

        public SchemaRepositorySearchTests(SchemaRepositoryFixture fixture)
        {
            _repo= fixture.Store;
        }


        [Fact]
        public void ItCanRetrieveMultipleSchemasForASingleOwner()
        {
            var results = _repo.GetSchemasByOwnerList(new string[] {"owner_0"}, 0, 10);
            results.Count.Should().Be(5);
            foreach (var r in results)
            {
                r.OwnerId.Should().Be("owner_0");
                r.SchemaId.Should().StartWith("schema_0.");
            }
        }

        [Fact]
        public void ItCanRetrieveMultipleSchemasForMultipleOwners()
        {
            var results = _repo.GetSchemasByOwnerList(new[] {"owner_1", "owner_2"}, 0, 10);
            results.Count.Should().Be(10);
            foreach (var r in results)
            {
                r.OwnerId.Should().BeOneOf("owner_1", "owner_2");
            }
        }

        [Fact]
        public void ItCanRetrieveMultipleSchemasForMultipleOwnersWithSkip()
        {
            var results = _repo.GetSchemasByOwnerList(new[] { "owner_1", "owner_2" }, 5, 10);
            results.Count.Should().Be(5);
            foreach (var r in results)
            {
                r.OwnerId.Should().BeOneOf("owner_1", "owner_2");
            }
        }

        [Fact]
        public void ItCanRetrieveMultipleSchemasForMultipleOwnersWithSkipAndTake()
        {
            var results = _repo.GetSchemasByOwnerList(new[] { "owner_1", "owner_2" }, 5, 3);
            results.Count.Should().Be(3);
            foreach (var r in results)
            {
                r.OwnerId.Should().BeOneOf("owner_1", "owner_2");
            }
        }

        [Fact]
        public void ItCanRetrieveMultipleSchemasForMultipleRepositories()
        {
            var results =
                _repo.GetSchemasByRepositoryList(new[] {"owner_1/repo_0", "owner_1/repo_1", "owner_1/repo_2"}, 0, 10);
            results.Count.Should().Be(3);
            foreach (var r in results)
            {
                r.OwnerId.Should().Be("owner_1");
                r.RepositoryId.Should().BeOneOf("owner_1/repo_0", "owner_1/repo_1", "owner_1/repo_2");
            }
        }

    }
}
