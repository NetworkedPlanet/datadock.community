﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Datadock.Common.Models;
using Datadock.Common.Stores;
using DataDock.Web.ViewComponents;
using DataDock.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Moq;
using Xunit;

namespace DataDock.Web.Tests.ViewComponents
{
    public class DatasetsViewComponentTests : BaseViewComponentTest
    {
        private readonly Mock<HttpContext> _mockHttpContext;
        private readonly Mock<IDatasetStore> _mockDatasetsStore;

        public DatasetsViewComponentTests()
        {
            _mockHttpContext = new Mock<HttpContext>();
            _mockDatasetsStore = new Mock<IDatasetStore>();
        }

        [Fact]
        public void NoOwnerIdReturnsEmptyView()
        {
            var vc = new DatasetsViewComponent(_mockDatasetsStore.Object);
            var asyncResult = vc.InvokeAsync(string.Empty, string.Empty);
            Assert.NotNull(asyncResult.Result);
            var result = asyncResult.Result as ViewViewComponentResult;
            Assert.NotNull(result);
            Assert.Equal("Empty", result.ViewName);
        }

        private List<DatasetInfo> GetDummyDatasets(string ownerId, string repoId, int num)
        {
            var datasets = new List<DatasetInfo>();
            for (var i = 0; i < num; i++)
            {
                var ds = new DatasetInfo
                {
                    OwnerId = ownerId,
                    RepositoryId = string.IsNullOrEmpty(repoId) ? $"{ownerId}-repo-{i}" : repoId,
                    DatasetId = Guid.NewGuid().ToString()
                };
                datasets.Add(ds);
            }
            return datasets;
        }

        [Fact]
        public void OwnerIdReturnsListView()
        {
            var ownerId = "owner-1";

            var datasets = GetDummyDatasets(ownerId, string.Empty, 10);
            _mockDatasetsStore.Setup(m => m.GetDatasetsForOwner("owner-1", It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.FromResult<IEnumerable<DatasetInfo>>(datasets));

            var vc = new DatasetsViewComponent(_mockDatasetsStore.Object);
            var asyncResult = vc.InvokeAsync(ownerId, string.Empty);
            Assert.NotNull(asyncResult.Result);
            var result = asyncResult.Result as ViewViewComponentResult;
            Assert.NotNull(result);

            _mockDatasetsStore.Verify(m => m.GetDatasetsForOwner(ownerId, 0 , 20), Times.Once);

            var model = result.ViewData?.Model as List<DatasetViewModel>;
            Assert.NotNull(model);
            Assert.Equal(10, model.Count);

            Assert.Equal("Default", result.ViewName);
        }

        [Fact]
        public void OwnerIdAndRepoIdReturnsListView()
        {
            var ownerId = "owner-1";
            var repoId = "repo-1";

            var datasets = GetDummyDatasets(ownerId, repoId, 10);
            _mockDatasetsStore.Setup(m => m.GetDatasetsForRepository("owner-1", "repo-1", It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.FromResult<IEnumerable<DatasetInfo>>(datasets));

            var vc = new DatasetsViewComponent(_mockDatasetsStore.Object);
            var asyncResult = vc.InvokeAsync(ownerId, repoId);
            Assert.NotNull(asyncResult.Result);
            var result = asyncResult.Result as ViewViewComponentResult;
            Assert.NotNull(result);

            _mockDatasetsStore.Verify(m => m.GetDatasetsForRepository(ownerId, repoId, 0, 20), Times.Once);

            var model = result.ViewData?.Model as List<DatasetViewModel>;
            Assert.NotNull(model);
            Assert.Equal(10, model.Count);

            Assert.Equal("Default", result.ViewName);
        }

        public void OwnerIdNoDatasetsFoundReturnsEmptyListView()
        {

        }

        public void OwnerIdAndRepoIdNoDatasetsFoundReturnsEmptyListView()
        {

        }

        public void ExceptionsDisplaysErrorView()
        {

        }
    }
}