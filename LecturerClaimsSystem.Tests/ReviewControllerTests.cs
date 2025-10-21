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
    /// Tests for Review Controller (Coordinator/Manager functionality)
    /// </summary>
    public class ReviewControllerTests
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

        private ReviewController CreateControllerWithSession(
            ApplicationDbContext context,
            string role = "Coordinator",
            string fullName = "Test Coordinator")
        {
            var controller = new ReviewController(context);

            var mockHttpContext = new Mock<HttpContext>();
            var mockSession = new Mock<ISession>();

            SetupSessionString(mockSession, "Role", role);
            SetupSessionString(mockSession, "FullName", fullName);

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
        public void Index_ReturnsView_WithAllClaims()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            context.Claims.AddRange(
                new Claim { LecturerName = "Lecturer 1", HoursWorked = 10, HourlyRate = 150 },
                new Claim { LecturerName = "Lecturer 2", HoursWorked = 8, HourlyRate = 200 }
            );
            context.SaveChanges();

            var controller = CreateControllerWithSession(context);

            // Act
            var result = controller.Index() as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsAssignableFrom<List<Claim>>(result.Model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        [Trait("Category", "Controller")]
        public void Index_RedirectsToLogin_WhenNotReviewer()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = CreateControllerWithSession(context, role: "Lecturer");

            // Act
            var result = controller.Index() as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Login", result.ActionName);
            Assert.Equal("Account", result.ControllerName);
        }

        [Fact]
        [Trait("Category", "Controller")]
        public void ApproveClaim_UpdatesStatus_ToApproved()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var claim = new Claim
            {
                LecturerName = "Test Lecturer",
                HoursWorked = 10,
                HourlyRate = 150,
                Status = "Pending"
            };
            context.Claims.Add(claim);
            context.SaveChanges();

            var controller = CreateControllerWithSession(context, "Coordinator", "Test Coordinator");

            // Act
            var result = controller.ApproveClaim(claim.ClaimId) as RedirectToActionResult;

            // Assert
            var updatedClaim = context.Claims.Find(claim.ClaimId);
            Assert.Equal("Approved", updatedClaim.Status);
            Assert.NotNull(updatedClaim.ReviewedDate);
            Assert.Equal("Test Coordinator", updatedClaim.ReviewedBy);
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
        }

        [Fact]
        [Trait("Category", "Controller")]
        public void RejectClaim_UpdatesStatus_ToRejected_WithReason()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var claim = new Claim
            {
                LecturerName = "Test Lecturer",
                HoursWorked = 10,
                HourlyRate = 150,
                Status = "Pending"
            };
            context.Claims.Add(claim);
            context.SaveChanges();

            var controller = CreateControllerWithSession(context, "Manager", "Test Manager");
            string rejectionReason = "Insufficient documentation provided";

            // Act
            var result = controller.RejectClaim(claim.ClaimId, rejectionReason) as RedirectToActionResult;

            // Assert
            var updatedClaim = context.Claims.Find(claim.ClaimId);
            Assert.Equal("Rejected", updatedClaim.Status);
            Assert.Equal(rejectionReason, updatedClaim.RejectionReason);
            Assert.NotNull(updatedClaim.ReviewedDate);
            Assert.Equal("Test Manager", updatedClaim.ReviewedBy);
            Assert.NotNull(result);
        }

        [Fact]
        [Trait("Category", "Controller")]
        public void ApproveClaim_ReturnsNotFound_WhenClaimDoesNotExist()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = CreateControllerWithSession(context);

            // Act
            var result = controller.ApproveClaim(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        [Trait("Category", "Controller")]
        public void RejectClaim_ReturnsNotFound_WhenClaimDoesNotExist()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = CreateControllerWithSession(context);

            // Act
            var result = controller.RejectClaim(999, "Test reason");

            // Assert
            Assert.IsType<NotFoundResult>(result);
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

        [Theory]
        [InlineData("Coordinator")]
        [InlineData("Manager")]
        [Trait("Category", "Controller")]
        public void Index_AllowsAccess_ForCoordinatorAndManager(string role)
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = CreateControllerWithSession(context, role);

            // Act
            var result = controller.Index() as ViewResult;

            // Assert
            Assert.NotNull(result);
        }
    }
}