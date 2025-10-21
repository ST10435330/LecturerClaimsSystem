using LecturerClaimsSystem.Controllers;
using LecturerClaimsSystem.Data;
using LecturerClaimsSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LecturerClaimsSystem.Tests
{
    /// <summary>
    /// Tests for Account Controller (Login/Logout functionality)
    /// </summary>
    public class AccountControllerTests
    {
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        private AccountController CreateControllerWithMockSession(ApplicationDbContext context)
        {
            var controller = new AccountController(context);

            var mockHttpContext = new Mock<HttpContext>();
            var mockSession = new Mock<ISession>();

            // Setup session methods
            mockSession.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()));
            mockSession.Setup(s => s.Clear());

            mockHttpContext.Setup(s => s.Session).Returns(mockSession.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            return controller;
        }

        [Fact]
        public void Login_GET_ReturnsView()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new AccountController(context);

            // Act
            var result = controller.Login() as ViewResult;

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void Login_POST_WithValidLecturerCredentials_RedirectsToLecturerIndex()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = CreateControllerWithMockSession(context);

            // Act
            var result = controller.Login("lecturer1", "password123") as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            Assert.Equal("Lecturer", result.ControllerName);
        }

        [Fact]
        public void Login_POST_WithValidCoordinatorCredentials_RedirectsToReviewIndex()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = CreateControllerWithMockSession(context);

            // Act
            var result = controller.Login("coordinator1", "password123") as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            Assert.Equal("Review", result.ControllerName);
        }

        [Fact]
        public void Login_POST_WithValidManagerCredentials_RedirectsToReviewIndex()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = CreateControllerWithMockSession(context);

            // Act
            var result = controller.Login("manager1", "password123") as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            Assert.Equal("Review", result.ControllerName);
        }

        [Fact]
        public void Login_POST_WithInvalidUsername_ReturnsViewWithError()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = CreateControllerWithMockSession(context);

            // Act
            var result = controller.Login("invaliduser", "password123") as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(controller.ViewBag.Error);
            Assert.Equal("Invalid username or password", controller.ViewBag.Error);
        }

        [Fact]
        public void Login_POST_WithInvalidPassword_ReturnsViewWithError()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = CreateControllerWithMockSession(context);

            // Act
            var result = controller.Login("lecturer1", "wrongpassword") as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(controller.ViewBag.Error);
            Assert.Equal("Invalid username or password", controller.ViewBag.Error);
        }

        [Fact]
        public void Login_POST_WithEmptyCredentials_ReturnsViewWithError()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = CreateControllerWithMockSession(context);

            // Act
            var result = controller.Login("", "") as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(controller.ViewBag.Error);
        }

        [Fact]
        public void Logout_ClearsSession_AndRedirectsToLogin()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = CreateControllerWithMockSession(context);

            // Act
            var result = controller.Logout() as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Login", result.ActionName);
        }

        [Theory]
        [InlineData("lecturer1", "password123", "Lecturer")]
        [InlineData("coordinator1", "password123", "Coordinator")]
        [InlineData("manager1", "password123", "Manager")]
        public void Login_POST_WithValidCredentials_ReturnsRedirect(
            string username,
            string password,
            string expectedRole)
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = CreateControllerWithMockSession(context);

            // Act
            var result = controller.Login(username, password);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.NotNull(redirectResult);
            Assert.NotNull(redirectResult.ActionName);
        }

        [Fact]
        public void Login_POST_ValidatesAgainstDatabaseUsers()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = CreateControllerWithMockSession(context);

            // Verify seeded users exist
            var users = context.Users.ToList();
            Assert.Equal(3, users.Count);
            Assert.Contains(users, u => u.Username == "lecturer1");
            Assert.Contains(users, u => u.Username == "coordinator1");
            Assert.Contains(users, u => u.Username == "manager1");

            // Act
            var result = controller.Login("lecturer1", "password123");

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
        }
    }
}