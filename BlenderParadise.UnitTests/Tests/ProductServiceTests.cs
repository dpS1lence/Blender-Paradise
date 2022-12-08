using BlenderParadise.Data.Models;
using BlenderParadise.Models;
using BlenderParadise.Repositories.Contracts;
using BlenderParadise.Services;
using BlenderParadise.Services.Contracts;
using BlenderParadise.Tests.Common;
using BlenderParadise.UnitTests.Mocks;
using BlenderParadise.UnitTests.Tests;
using Microsoft.AspNetCore.Identity;
using MockQueryable.Moq;
using Moq;

namespace BlenderParadise.UnitTests
{
    public class ProductServiceTests : TestsBase
    {
        private Mock<IRepository>? repoMock;
        private readonly BlenderParadiseTestDb testDb = new();

        [Test]
        public void ProductService_GetAllAsync_Should_Get_All_Products()
        {
            var products = testDb.products.ToList();

            var categories = testDb.categories.ToList();

            repoMock = new Mock<IRepository>();
            repoMock.Setup(r => r.All<Product>()).Returns(products.AsQueryable());
            repoMock.Setup(r => r.GetByIdAsync<Category>(It.IsAny<int>()))!.ReturnsAsync((int id) => categories.FirstOrDefault(a => a.Id == id));

            IProductService service = new ProductService(repoMock.Object, this.userManager.Object);

            var actual = service.GetAllAsync();

            repoMock.VerifyAll();
            Assert.That(actual.Result, Is.Not.Null);
            Assert.IsAssignableFrom<List<ViewProductModel>>(actual.Result);
            Assert.That(actual.Result, Has.Count.EqualTo(4));
        }

        [Test]
        public void ProductService_GetOneAsync_Should_Get_One_Product()
        {
            var products = testDb.products.ToList();

            var categories = testDb.categories.ToList();

            var photos = testDb.photos.ToList();

            repoMock = new Mock<IRepository>();
            repoMock.Setup(r => r.GetByIdAsync<Product>(It.IsAny<int>()))!.ReturnsAsync((int id) => products.FirstOrDefault(a => a.Id == id));
            repoMock.Setup(r => r.GetByIdAsync<Category>(It.IsAny<int>()))!.ReturnsAsync((int id) => categories.FirstOrDefault(a => a.Id == id));
            repoMock.Setup(r => r.All<Photo>()).Returns(photos.AsQueryable());

            IProductService service = new ProductService(repoMock.Object, this.userManager.Object);

            var actual = service.GetOneAsync(2);

            repoMock.VerifyAll();
            Assert.That(actual.Result, Is.Not.Null);
            Assert.IsAssignableFrom<DownloadProductModel>(actual.Result);
        }
    }
}