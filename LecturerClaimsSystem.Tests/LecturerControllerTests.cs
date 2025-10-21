using LecturerClaimsSystem.Controllers;
using LecturerClaimsSystem.Data;
using LecturerClaimsSystem.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LecturerClaimsSystem.Tests
{
    /// <summary>
    /// Tests for Lecturer Controller functionality
    /// </summary>
    public class LecturerControllerTests
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

        private LecturerController CreateControllerWithSession(
            ApplicationDbContext context,
            string role = "Lecturer",
            string fullName = "Test Lecturer")
        {
            var mockEnvironment = new Mock<IWebHostEnvironment>();
            mockEnvironment.Setup(m => m.WebRootPath).Returns("C:\\TestPath");

            var controller = new LecturerController(context, mockEnvironment.Object);

            // Setup Session
            var mockHttpContext = new Mock<HttpContext>();
            var mockSession = new Mock<ISession>();

            // Setup session values
            SetupSessionString(mockSession, "Role", role);
            SetupSessionString(mockSession, "FullName", fullName);
            SetupSessionString(mockSession, "Username", "testuser");

            mockHttpContext.Setup(s => s.Session).Returns(mockSession.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            return controller;
        }

        private void SetupSessionString(Mock<ISession> mockSession, string key, string value)
        {
            mockSession.Setup(x => x.TryGetValue(key, out It.Ref<byte[]>.IsAny))
                .Callback((string k, out byte[] v) =>
                {
                    v = System.Text.Encoding.UTF8.GetBytes(value);
                })
                .Returns(true);
        }

        [Fact]
        [Trait("Category", "Controller")]
        public void Index_ReturnsView_WithClaimsForLecturer()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            context.Claims.AddRange(
                new Claim { LecturerName = "Test Lecturer", HoursWorked = 10, HourlyRate = 150 },
                new Claim { LecturerName = "Other Lecturer", HoursWorked = 8, HourlyRate = 200 }
            );
            context.SaveChanges();

            var controller = CreateControllerWithSession(context);

            // Act
            var result = controller.Index() as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsAssignableFrom<List<Claim>>(result.Model);
            Assert.Single(model);
            Assert.Equal("Test Lecturer", model[0].LecturerName);
        }

        [Fact]
        [Trait("Category", "Controller")]
        public void Index_RedirectsToLogin_WhenNotLecturer()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = CreateControllerWithSession(context, role: "InvalidRole");

            // Act
            var result = controller.Index() as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Login", result.ActionName);
            Assert.Equal("Account", result.ControllerName);
        }

        [Fact]
        [Trait("Category", "Controller")]
        public void SubmitClaim_GET_ReturnsView_ForLecturer()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = CreateControllerWithSession(context);

            // Act
            var result = controller.SubmitClaim() as ViewResult;

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        [Trait("Category", "Controller")]
        public void SubmitClaim_GET_RedirectsToLogin_WhenNotLecturer()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockEnvironment = new Mock<IWebHostEnvironment>();
            var controller = new LecturerController(context, mockEnvironment.Object);

            var mockHttpContext = new Mock<HttpContext>();
            var mockSession = new Mock<ISession>();
            mockHttpContext.Setup(s => s.Session).Returns(mockSession.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = controller.SubmitClaim() as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Login", result.ActionName);
        }

        [Fact]
        [Trait("Category", "Controller")]
        public void ViewClaim_ReturnsView_WithCorrectClaim()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var claim = new Claim
            {
                LecturerName = "Test Lecturer",
                HoursWorked = 10,
                HourlyRate = 150
            };
            context.Claims.Add(claim);
            context.SaveChanges();

            var controller = CreateControllerWithSession(context);

            // Act
            var result = controller.ViewClaim(claim.ClaimId) as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<Claim>(result.Model);
            Assert.Equal(claim.ClaimId, model.ClaimId);
        }

        [Fact]
        [Trait("Category", "Controller")]
        public void ViewClaim_ReturnsNotFound_WhenClaimDoesNotExist()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = CreateControllerWithSession(context);

            // Act
            var result = controller.ViewClaim(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}