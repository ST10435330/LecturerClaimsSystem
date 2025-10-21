using System.ComponentModel.DataAnnotations;

namespace LecturerClaimsSystem.Models
{
    public class Claim
    {
        public int ClaimId { get; set; }

        [Required]
        public string LecturerName { get; set; }

        [Required]
        [Range(1, 744, ErrorMessage = "Hours worked must be between 1 and 744")]
        public decimal HoursWorked { get; set; }

        [Required]
        [Range(1, 10000, ErrorMessage = "Hourly rate must be between 1 and 10000")]
        public decimal HourlyRate { get; set; }

        public string? AdditionalNotes { get; set; }

        public string? DocumentPath { get; set; }

        public string? OriginalFileName { get; set; }

        [Required]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public DateTime SubmittedDate { get; set; } = DateTime.Now;

        public DateTime? ReviewedDate { get; set; }

        public string? ReviewedBy { get; set; }

        public string? RejectionReason { get; set; }

        public decimal TotalAmount => HoursWorked * HourlyRate;
    }
}