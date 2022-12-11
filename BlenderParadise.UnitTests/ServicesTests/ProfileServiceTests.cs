﻿using BlenderParadise.Data.Models;
using MockQueryable.Moq;

namespace BlenderParadise.UnitTests.ServicesTests
{
    [TestFixture]
    public class ProfileServiceTests : TestsBase
    {
        [Test]
        public void GetUserData_Should_Get_User_Data()
        {
            // Set up mock repository
            repoMock = new Mock<IRepository>();
            repoMock.Setup(r => r.GetByIdAsync<Product>(It.IsAny<int>()))!.ReturnsAsync((int id) => products.FirstOrDefault(a => a.Id == id));
            repoMock.Setup(r => r.GetByIdAsync<Category>(It.IsAny<int>()))!.ReturnsAsync((int id) => categories.FirstOrDefault(a => a.Id == id));

            // Create file service and profile service
            IFileService fileService = new LocalStorageFileService("");
            IProfileService service = new ProfileService(repoMock.Object, fileService, this.userManager.Object);

            // Call the method being tested and store the result
            var actual = service.GetUserData(testDb.User.UserName);

            // Verify that the mock repository was called as expected
            repoMock.VerifyAll();

            // Assert that the result is not null and has the expected type
            Assert.That(actual.Result, Is.Not.Null);
            Assert.IsAssignableFrom<UserProfileModel>(actual.Result);

            // Assert that the user name and product count are correct
            Assert.Multiple(() =>
            {
                Assert.That(actual.Result.UserName, Is.EqualTo(testDb.User.UserName));
                Assert.That(actual.Result.Bio, Is.EqualTo(testDb.User.Description));
                Assert.That(actual.Result.ProfilePhoto, Is.EqualTo(testDb.User.ProfilePicture));
                Assert.That(actual.Result.Id, Is.EqualTo(testDb.User.Id));
                Assert.That(actual.Result.UserModels.First().Name, Is.EqualTo(testDb.User.ProductsData.First().Name));
                Assert.That(actual.Result.UserModels.First().CoverPhoto, Is.EqualTo("data:image/jpg;base64,"));
                Assert.That(actual.Result.UserModels, Has.Count.EqualTo(testDb.User.ProductsData.Count));
            });

            for (int i = 0; i < actual.Result.UserModels.Count; i++)
            {
                Product? product = testDb.products.FirstOrDefault(a => a.Id == actual.Result.UserModels[i].Id);

                Assert.That(actual.Result.UserModels[i].Category, Is.EqualTo(testDb.categories.FirstOrDefault(a => a.Id == product?.CategoryId)?.Name));
            }
        }

        [Test]
        public void GetUserData_Should_Throw_ArgumentException_If_User_Is_Null()
        {
            repoMock = new Mock<IRepository>();

            IFileService fileService = new LocalStorageFileService("");
            IProfileService service = new ProfileService(repoMock.Object, fileService, this.userManager.Object);

            var actual = service.GetUserData("invalid name");

            Assert.ThrowsAsync<ArgumentException>(() => service.GetUserData("invalid name"), "Invalid username.");
        }

        [Test]
        public void GetUserData_Should_Throw_ArgumentException_If_Product_Is_Null()
        {
            testDb.User.ProductsData.First().CategoryId = -1;

            repoMock = new Mock<IRepository>();
            repoMock.Setup(r => r.GetByIdAsync<Product>(It.IsAny<int>()))!.ReturnsAsync((int id) => products.FirstOrDefault(a => a.Id == id));
            repoMock.Setup(r => r.GetByIdAsync<Category>(It.IsAny<int>()))!.ReturnsAsync((int id) => categories.FirstOrDefault(a => a.Id == id));

            IFileService fileService = new LocalStorageFileService("");
            IProfileService service = new ProfileService(repoMock.Object, fileService, this.userManager.Object);

            Assert.ThrowsAsync<ArgumentException>(() => service.GetUserData(testDb.User.UserName));
        }

        [Test]
        public void GetUserById_Should_Get_User_By_Id()
        {
            repoMock = new Mock<IRepository>();
            IFileService fileService = new LocalStorageFileService("");
            IProfileService service = new ProfileService(repoMock.Object, fileService, this.userManager.Object);

            var actual = service.GetUserById(testDb.User.Id);

            repoMock.VerifyAll();
            Assert.That(actual.Result, Is.Not.Null);
            Assert.That(actual.Result.Id, Is.EqualTo(testDb.User.Id));
        }

        [Test]
        public void RemoveUserUploadAsync_Should_Remove_User_Upload()
        {
            var content = testDb.content.ToList();

            var photos = testDb.photos.ToList();

            repoMock = new Mock<IRepository>();
            repoMock.Setup(r => r.Delete(It.IsAny<Photo>()));
            repoMock.Setup(r => r.Delete(It.IsAny<Content>()));
            repoMock.Setup(r => r.Delete(It.IsAny<Product>()));
            repoMock.Setup(r => r.All<Photo>()).Returns(photos.BuildMock());
            repoMock.Setup(r => r.GetByIdAsync<Product>(It.IsAny<int>()))!.ReturnsAsync((int id) => products.FirstOrDefault(a => a.Id == id));
            repoMock.Setup(r => r.GetByIdAsync<Content>(It.IsAny<int>()))!.ReturnsAsync((int id) => content.FirstOrDefault(a => a.Id == id));
            repoMock.Setup(r => r.GetByIdAsync<Category>(It.IsAny<int>()))!.ReturnsAsync((int id) => categories.FirstOrDefault(a => a.Id == id));
            IProfileService service = new ProfileService(repoMock.Object, this.fileService, this.userManager.Object);

            int beforeRemoveCount = testDb.User.ProductsData.Count;

            var actual = service.RemoveUserUploadAsync(testDb.User.Id, testDb.User.ProductsData.ToList().First().Id);

            repoMock.VerifyAll();
            Assert.That(actual.Result, Is.Not.Null);
            Assert.IsAssignableFrom<UserProfileModel>(actual.Result);
            Assert.That(actual.Result.UserName, Is.EqualTo(testDb.User.UserName));
        }

        [Test]
        public void RemoveUserUploadAsync_Should_Throw_ArgumentException_If_User_Is_Invalid()
        {
            repoMock = new Mock<IRepository>();

            IProfileService service = new ProfileService(repoMock.Object, this.fileService, this.userManager.Object);

            Assert.ThrowsAsync<ArgumentException>(() => service.RemoveUserUploadAsync("-1", testDb.User.ProductsData.First().Id));
        }

        [Test]
        public void RemoveUserUploadAsync_Should_Throw_ArgumentException_If_User_Product_Id_Is_Invalid()
        {
            repoMock = new Mock<IRepository>();

            IProfileService service = new ProfileService(repoMock.Object, this.fileService, this.userManager.Object);

            Assert.ThrowsAsync<ArgumentException>(() => service.RemoveUserUploadAsync(testDb.User.Id, testDb.User2.ProductsData.First().Id));
        }

        [Test]
        public void RemoveUserUploadAsync_Should_Throw_ArgumentException_If_Repo_Product_ProductId_Is_Invalid()
        {
            var products = new List<Product>();

            repoMock = new Mock<IRepository>();
            repoMock.Setup(r => r.GetByIdAsync<Product>(It.IsAny<int>()))!.ReturnsAsync((int id) => products.FirstOrDefault(a => a.Id == id));

            IProfileService service = new ProfileService(repoMock.Object, this.fileService, this.userManager.Object);

            Assert.ThrowsAsync<ArgumentException>(() => service.RemoveUserUploadAsync(testDb.User.Id, testDb.User.ProductsData.First().Id));
        }

        [Test]
        public void RemoveUserUploadAsync_Should_Throw_ArgumentException_If_ContentId_Is_Invalid()
        {
            var products = new List<Product>
            {
                new Product { Id = 1, Name = "Product1", CategoryId = 1, Description = "Description", Geometry = 10, Polygons = 10, Vertices = 10,Photo = Array.Empty<byte>(), UserId =  "1", ContentId = -1 },
               new Product { Id = 2, Name = "Product2", CategoryId = 2, Description = "Description", Geometry = 10, Polygons = 10, Vertices = 10,Photo = Array.Empty<byte>(), UserId =  "3", ContentId = -1 }
            };

            var content = testDb.content.ToList();

            repoMock = new Mock<IRepository>();
            repoMock.Setup(r => r.GetByIdAsync<Product>(It.IsAny<int>()))!.ReturnsAsync((int id) => products.FirstOrDefault(a => a.Id == id));
            repoMock.Setup(r => r.GetByIdAsync<Content>(It.IsAny<int>()))!.ReturnsAsync((int id) => content.FirstOrDefault(a => a.Id == id));

            IProfileService service = new ProfileService(repoMock.Object, this.fileService, this.userManager.Object);

            Assert.ThrowsAsync<ArgumentException>(() => service.RemoveUserUploadAsync(testDb.User.Id, testDb.User.ProductsData.First().Id));
        }

        [Test]
        public void RemoveUserUploadAsync_Should_Throw_ArgumentException_If_No_Photo_Found()
        {
            var content = testDb.content.ToList();

            var invalidPhotos = testDb.invalidPhotos.ToList();

            repoMock = new Mock<IRepository>();
            repoMock.Setup(r => r.GetByIdAsync<Product>(It.IsAny<int>()))!.ReturnsAsync((int id) => products.FirstOrDefault(a => a.Id == id));
            repoMock.Setup(r => r.GetByIdAsync<Content>(It.IsAny<int>()))!.ReturnsAsync((int id) => content.FirstOrDefault(a => a.Id == id));
            repoMock.Setup(r => r.GetByIdAsync<Category>(It.IsAny<int>()))!.ReturnsAsync((int id) => categories.FirstOrDefault(a => a.Id == id));
            repoMock.Setup(r => r.All<Photo>()).Returns(invalidPhotos.BuildMock());

            IProfileService service = new ProfileService(repoMock.Object, this.fileService, this.userManager.Object);

            Assert.ThrowsAsync<ArgumentException>(() => service.RemoveUserUploadAsync(testDb.User.Id, testDb.User.ProductsData.First().Id));
        }

        [Test]
        public void EditUserUploadAsync_Should_Return_View_Model()
        {
            var products = testDb.products.ToList();

            repoMock = new Mock<IRepository>();
            repoMock.Setup(r => r.GetByIdAsync<Product>(It.IsAny<int>()))!.ReturnsAsync((int id) => products.FirstOrDefault(a => a.Id == id));
            IProfileService service = new ProfileService(repoMock.Object, this.fileService, this.userManager.Object);

            int beforeRemoveCount = testDb.User.ProductsData.Count;

            var actual = service.EditUserUploadAsync(products.First().Id);

            repoMock.VerifyAll();
            Assert.That(actual.Result, Is.Not.Null);
            Assert.IsAssignableFrom<EditProductModel>(actual.Result);
            Assert.That(actual.Result.Name, Is.EqualTo(testDb.products.First().Name));
        }

        [Test]
        public void EditUserUploadAsync_Should_Update_User_Upload()
        {
            var model = new EditProductModel()
            {
                Id = 1,
                Name = "Name",
                Description = "Description"
            };

            repoMock = new Mock<IRepository>();
            repoMock.Setup(r => r.GetByIdAsync<Product>(It.IsAny<int>()))!.ReturnsAsync((int id) => products.FirstOrDefault(a => a.Id == id));
            IProfileService service = new ProfileService(repoMock.Object, this.fileService, this.userManager.Object);

            int beforeRemoveCount = testDb.User.ProductsData.Count;

            var actual = service.EditUserUploadAsync(model);

            repoMock.VerifyAll();
            Assert.That(actual.Result, Is.Not.EqualTo(false));
            Assert.IsAssignableFrom<bool>(actual.Result);
        }
    }
}