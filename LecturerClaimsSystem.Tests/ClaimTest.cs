using LecturerClaimsSystem.Models;
using Xunit;

namespace LecturerClaimsSystem.Tests
{
    /// <summary>
    /// Tests for Claim model - Testing calculation and business logic
    /// </summary>
    public class ClaimTests
    {
        [Fact]
        public void TotalAmount_CalculatesCorrectly()
        {
            // Arrange
            var claim = new Claim
            {
                HoursWorked = 10,
                HourlyRate = 150
            };

            // Act
            var total = claim.TotalAmount;

            // Assert
            Assert.Equal(1500, total);
        }

        [Fact]
        public void TotalAmount_WithDecimalHours_CalculatesCorrectly()
        {
            // Arrange
            var claim = new Claim
            {
                HoursWorked = 7.5m,
                HourlyRate = 200
            };

            // Act
            var total = claim.TotalAmount;

            // Assert
            Assert.Equal(1500, total);
        }

        [Fact]
        public void Claim_DefaultStatus_IsPending()
        {
            // Arrange & Act
            var claim = new Claim
            {
                LecturerName = "Test Lecturer",
                HoursWorked = 5,
                HourlyRate = 100
            };

            // Assert
            Assert.Equal("Pending", claim.Status);
        }

        [Fact]
        public void Claim_SubmittedDate_IsSetAutomatically()
        {
            // Arrange
            var beforeCreation = DateTime.Now.AddSeconds(-1);

            // Act
            var claim = new Claim
            {
                LecturerName = "Test Lecturer",
                HoursWorked = 5,
                HourlyRate = 100
            };

            var afterCreation = DateTime.Now.AddSeconds(1);

            // Assert
            Assert.True(claim.SubmittedDate >= beforeCreation);
            Assert.True(claim.SubmittedDate <= afterCreation);
        }

        [Theory]
        [InlineData(8, 150, 1200)]
        [InlineData(10, 200, 2000)]
        [InlineData(5.5, 180, 990)]
        [InlineData(1, 100, 100)]
        public void TotalAmount_VariousScenarios_CalculatesCorrectly(
            decimal hours, decimal rate, decimal expectedTotal)
        {
            // Arrange
            var claim = new Claim
            {
                HoursWorked = hours,
                HourlyRate = rate
            };

            // Act
            var total = claim.TotalAmount;

            // Assert
            Assert.Equal(expectedTotal, total);
        }
    }
}