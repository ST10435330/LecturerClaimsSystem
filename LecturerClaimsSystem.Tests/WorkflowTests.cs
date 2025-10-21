using LecturerClaimsSystem.Data;
using LecturerClaimsSystem.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LecturerClaimsSystem.Tests
{
    /// <summary>
    /// Tests for complete claim workflows - End-to-end scenarios
    /// </summary>
    public class WorkflowTests
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

        [Fact]
        public void ClaimWorkflow_SubmitAndApprove_WorksCorrectly()
        {
            // Arrange
            using var context = GetInMemoryDbContext();

            // Act - Step 1: Lecturer submits claim
            var claim = new Claim
            {
                LecturerName = "John Smith",
                HoursWorked = 10,
                HourlyRate = 150,
                AdditionalNotes = "Teaching Programming 101",
                Status = "Pending",
                SubmittedDate = DateTime.Now
            };
            context.Claims.Add(claim);
            context.SaveChanges();

            // Act - Step 2: Coordinator approves claim
            var pendingClaim = context.Claims.First(c => c.Status == "Pending");
            pendingClaim.Status = "Approved";
            pendingClaim.ReviewedDate = DateTime.Now;
            pendingClaim.ReviewedBy = "Jane Coordinator";
            context.SaveChanges();

            // Assert
            var approvedClaim = context.Claims.Find(claim.ClaimId);
            Assert.NotNull(approvedClaim);
            Assert.Equal("Approved", approvedClaim.Status);
            Assert.Equal("Jane Coordinator", approvedClaim.ReviewedBy);
            Assert.NotNull(approvedClaim.ReviewedDate);
            Assert.Equal(1500, approvedClaim.TotalAmount);
        }

        [Fact]
        public void ClaimWorkflow_SubmitAndReject_WorksCorrectly()
        {
            // Arrange
            using var context = GetInMemoryDbContext();

            // Act - Step 1: Lecturer submits claim
            var claim = new Claim
            {
                LecturerName = "John Smith",
                HoursWorked = 100, // Suspicious hours
                HourlyRate = 150,
                Status = "Pending"
            };
            context.Claims.Add(claim);
            context.SaveChanges();

            // Act - Step 2: Manager rejects claim
            var pendingClaim = context.Claims.First(c => c.Status == "Pending");
            pendingClaim.Status = "Rejected";
            pendingClaim.ReviewedDate = DateTime.Now;
            pendingClaim.ReviewedBy = "Bob Manager";
            pendingClaim.RejectionReason = "Hours worked exceeds reasonable limits. Please provide documentation.";
            context.SaveChanges();

            // Assert
            var rejectedClaim = context.Claims.Find(claim.ClaimId);
            Assert.NotNull(rejectedClaim);
            Assert.Equal("Rejected", rejectedClaim.Status);
            Assert.Equal("Bob Manager", rejectedClaim.ReviewedBy);
            Assert.NotNull(rejectedClaim.RejectionReason);
            Assert.Contains("documentation", rejectedClaim.RejectionReason);
        }

        [Fact]
        public void ClaimTracking_StatusChanges_AreTrackedCorrectly()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var claim = new Claim
            {
                LecturerName = "Test Lecturer",
                HoursWorked = 8,
                HourlyRate = 175,
                Status = "Pending"
            };
            context.Claims.Add(claim);
            context.SaveChanges();

            // Act & Assert - Initial Status
            var trackedClaim = context.Claims.Find(claim.ClaimId);
            Assert.Equal("Pending", trackedClaim.Status);
            Assert.Null(trackedClaim.ReviewedDate);
            Assert.Null(trackedClaim.ReviewedBy);

            // Act & Assert - After Approval
            trackedClaim.Status = "Approved";
            trackedClaim.ReviewedDate = DateTime.Now;
            trackedClaim.ReviewedBy = "Coordinator";
            context.SaveChanges();

            var approvedClaim = context.Claims.Find(claim.ClaimId);
            Assert.Equal("Approved", approvedClaim.Status);
            Assert.NotNull(approvedClaim.ReviewedDate);
            Assert.Equal("Coordinator", approvedClaim.ReviewedBy);
        }

        [Fact]
        public void MultipleClaims_ForSameLecturer_AreHandledCorrectly()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var lecturer = "John Smith";

            // Act - Submit multiple claims
            context.Claims.AddRange(
                new Claim { LecturerName = lecturer, HoursWorked = 10, HourlyRate = 150, Status = "Pending" },
                new Claim { LecturerName = lecturer, HoursWorked = 8, HourlyRate = 150, Status = "Pending" },
                new Claim { LecturerName = lecturer, HoursWorked = 12, HourlyRate = 150, Status = "Pending" }
            );
            context.SaveChanges();

            // Approve first claim
            var firstClaim = context.Claims.First(c => c.LecturerName == lecturer);
            firstClaim.Status = "Approved";
            firstClaim.ReviewedBy = "Coordinator";
            context.SaveChanges();

            // Reject second claim
            var secondClaim = context.Claims
                .Where(c => c.LecturerName == lecturer && c.Status == "Pending")
                .First();
            secondClaim.Status = "Rejected";
            secondClaim.ReviewedBy = "Manager";
            secondClaim.RejectionReason = "Missing documentation";
            context.SaveChanges();

            // Assert
            var allClaims = context.Claims.Where(c => c.LecturerName == lecturer).ToList();
            Assert.Equal(3, allClaims.Count);
            Assert.Single(allClaims.Where(c => c.Status == "Approved"));
            Assert.Single(allClaims.Where(c => c.Status == "Rejected"));
            Assert.Single(allClaims.Where(c => c.Status == "Pending"));
        }

        [Fact]
        public void ClaimStatistics_CalculateCorrectly()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            context.Claims.AddRange(
                new Claim { LecturerName = "L1", HoursWorked = 10, HourlyRate = 100, Status = "Pending" },
                new Claim { LecturerName = "L2", HoursWorked = 10, HourlyRate = 100, Status = "Pending" },
                new Claim { LecturerName = "L3", HoursWorked = 10, HourlyRate = 100, Status = "Approved" },
                new Claim { LecturerName = "L4", HoursWorked = 10, HourlyRate = 100, Status = "Approved" },
                new Claim { LecturerName = "L5", HoursWorked = 10, HourlyRate = 100, Status = "Approved" },
                new Claim { LecturerName = "L6", HoursWorked = 10, HourlyRate = 100, Status = "Rejected" }
            );
            context.SaveChanges();

            // Act
            var totalClaims = context.Claims.Count();
            var pendingCount = context.Claims.Count(c => c.Status == "Pending");
            var approvedCount = context.Claims.Count(c => c.Status == "Approved");
            var rejectedCount = context.Claims.Count(c => c.Status == "Rejected");
            var totalApprovedAmount = context.Claims
                .Where(c => c.Status == "Approved")
                .Sum(c => c.TotalAmount);

            // Assert
            Assert.Equal(6, totalClaims);
            Assert.Equal(2, pendingCount);
            Assert.Equal(3, approvedCount);
            Assert.Equal(1, rejectedCount);
            Assert.Equal(3000, totalApprovedAmount);
        }

        [Fact]
        public void DocumentUpload_IsTrackedCorrectly()
        {
            // Arrange
            using var context = GetInMemoryDbContext();

            // Act
            var claimWithDocument = new Claim
            {
                LecturerName = "Test Lecturer",
                HoursWorked = 10,
                HourlyRate = 150,
                DocumentPath = "/uploads/test-document.pdf",
                OriginalFileName = "timesheet.pdf",
                Status = "Pending"
            };
            context.Claims.Add(claimWithDocument);
            context.SaveChanges();

            // Assert
            var savedClaim = context.Claims.Find(claimWithDocument.ClaimId);
            Assert.NotNull(savedClaim.DocumentPath);
            Assert.Equal("/uploads/test-document.pdf", savedClaim.DocumentPath);
            Assert.Equal("timesheet.pdf", savedClaim.OriginalFileName);
        }
    }
}