using LecturerClaimsSystem.Data;
using LecturerClaimsSystem.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LecturerClaimsSystem.Tests
{
    /// <summary>
    /// Tests for database operations - CRUD functionality
    /// </summary>
    public class DatabaseTests
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
        public void CanAddClaimToDatabase()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var claim = new Claim
            {
                LecturerName = "John Smith",
                HoursWorked = 10,
                HourlyRate = 150,
                Status = "Pending"
            };

            // Act
            context.Claims.Add(claim);
            context.SaveChanges();

            // Assert
            Assert.Equal(1, context.Claims.Count());
            var savedClaim = context.Claims.First();
            Assert.Equal("John Smith", savedClaim.LecturerName);
            Assert.Equal(10, savedClaim.HoursWorked);
        }

        [Fact]
        public void CanRetrieveClaimById()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var claim = new Claim
            {
                LecturerName = "Jane Doe",
                HoursWorked = 8,
                HourlyRate = 200
            };
            context.Claims.Add(claim);
            context.SaveChanges();

            // Act
            var retrievedClaim = context.Claims.Find(claim.ClaimId);

            // Assert
            Assert.NotNull(retrievedClaim);
            Assert.Equal("Jane Doe", retrievedClaim.LecturerName);
            Assert.Equal(8, retrievedClaim.HoursWorked);
        }

        [Fact]
        public void CanUpdateClaimStatus()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var claim = new Claim
            {
                LecturerName = "Test Lecturer",
                HoursWorked = 10,
                HourlyRate = 150,
                Status = "Pending"
            };
            context.Claims.Add(claim);
            context.SaveChanges();

            // Act
            claim.Status = "Approved";
            claim.ReviewedBy = "Test Coordinator";
            claim.ReviewedDate = DateTime.Now;
            context.SaveChanges();

            // Assert
            var updatedClaim = context.Claims.Find(claim.ClaimId);
            Assert.Equal("Approved", updatedClaim.Status);
            Assert.Equal("Test Coordinator", updatedClaim.ReviewedBy);
            Assert.NotNull(updatedClaim.ReviewedDate);
        }

        [Fact]
        public void CanFilterClaimsByStatus()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            context.Claims.AddRange(
                new Claim { LecturerName = "L1", HoursWorked = 10, HourlyRate = 150, Status = "Pending" },
                new Claim { LecturerName = "L2", HoursWorked = 8, HourlyRate = 200, Status = "Approved" },
                new Claim { LecturerName = "L3", HoursWorked = 5, HourlyRate = 180, Status = "Pending" },
                new Claim { LecturerName = "L4", HoursWorked = 12, HourlyRate = 160, Status = "Rejected" }
            );
            context.SaveChanges();

            // Act
            var pendingClaims = context.Claims.Where(c => c.Status == "Pending").ToList();
            var approvedClaims = context.Claims.Where(c => c.Status == "Approved").ToList();
            var rejectedClaims = context.Claims.Where(c => c.Status == "Rejected").ToList();

            // Assert
            Assert.Equal(2, pendingClaims.Count);
            Assert.Single(approvedClaims);
            Assert.Single(rejectedClaims);
        }

        [Fact]
        public void CanFilterClaimsByLecturer()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            context.Claims.AddRange(
                new Claim { LecturerName = "John Smith", HoursWorked = 10, HourlyRate = 150 },
                new Claim { LecturerName = "Jane Doe", HoursWorked = 8, HourlyRate = 200 },
                new Claim { LecturerName = "John Smith", HoursWorked = 5, HourlyRate = 180 }
            );
            context.SaveChanges();

            // Act
            var johnsClaims = context.Claims
                .Where(c => c.LecturerName == "John Smith")
                .ToList();

            // Assert
            Assert.Equal(2, johnsClaims.Count);
            Assert.All(johnsClaims, c => Assert.Equal("John Smith", c.LecturerName));
        }

        [Fact]
        public void DatabaseSeedsDefaultUsers()
        {
            // Arrange & Act
            using var context = GetInMemoryDbContext();

            // Assert
            Assert.True(context.Users.Any());
            Assert.Contains(context.Users, u => u.Username == "lecturer1");
            Assert.Contains(context.Users, u => u.Username == "coordinator1");
            Assert.Contains(context.Users, u => u.Username == "manager1");
            Assert.Equal(3, context.Users.Count());
        }
    }
}