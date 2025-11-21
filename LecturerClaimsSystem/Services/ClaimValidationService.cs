using LecturerClaimsSystem.Models;

namespace LecturerClaimsSystem.Services
{
    public class ClaimValidationService
    {
        public ClaimValidationResult ValidateClaim(Claim claim)
        {
            var result = new ClaimValidationResult
            {
                IsValid = true,
                Warnings = new List<string>(),
                Errors = new List<string>(),
                RequiresManagerApproval = false,
                AutoApprovalEligible = false
            };

            // Rule 1: Hours validation
            if (claim.HoursWorked > 160)
            {
                result.Warnings.Add($"Hours worked ({claim.HoursWorked}) exceeds standard monthly limit (160 hours)");
                result.RequiresManagerApproval = true;
            }

            if (claim.HoursWorked < 1)
            {
                result.Warnings.Add("Very low hours claimed");
            }

            if (claim.HoursWorked > 744)
            {
                result.Errors.Add("Hours exceed maximum possible hours in a month (744)");
                result.IsValid = false;
            }

            // Rule 2: Rate validation
            if (claim.HourlyRate > 1000)
            {
                result.Warnings.Add($"Hourly rate (R{claim.HourlyRate}) is unusually high");
                result.RequiresManagerApproval = true;
            }

            if (claim.HourlyRate < 100)
            {
                result.Warnings.Add($"Hourly rate (R{claim.HourlyRate}) is below recommended minimum");
            }

            // Rule 3: Total amount validation
            if (claim.TotalAmount > 50000)
            {
                result.Warnings.Add($"Total amount (R{claim.TotalAmount:N2}) exceeds R50,000");
                result.RequiresManagerApproval = true;
            }

            if (claim.TotalAmount > 100000)
            {
                result.Errors.Add("Total amount exceeds maximum allowed (R100,000)");
                result.IsValid = false;
            }

            // Rule 4: Document requirements
            if (claim.TotalAmount > 5000 && string.IsNullOrEmpty(claim.DocumentPath))
            {
                result.Warnings.Add("Supporting documentation missing for claim over R5,000");
            }

            // Rule 5: Auto-approval eligibility
            if (claim.HoursWorked <= 40 &&
                claim.HourlyRate >= 150 &&
                claim.HourlyRate <= 500 &&
                claim.TotalAmount <= 10000 &&
                !string.IsNullOrEmpty(claim.DocumentPath))
            {
                result.AutoApprovalEligible = true;
            }

            // Rule 6: Submission date validation
            var daysSinceSubmission = (DateTime.Now - claim.SubmittedDate).Days;
            if (daysSinceSubmission > 30)
            {
                result.Warnings.Add($"Claim is {daysSinceSubmission} days old - urgent review required");
            }

            // Calculate risk score
            result.RiskScore = CalculateRiskScore(claim);

            return result;
        }

        private int CalculateRiskScore(Claim claim)
        {
            int score = 0;

            // Higher risk for excessive hours
            if (claim.HoursWorked > 160) score += 30;
            if (claim.HoursWorked > 200) score += 20;

            // Higher risk for unusual rates
            if (claim.HourlyRate > 800) score += 25;
            if (claim.HourlyRate < 100) score += 15;

            // Higher risk for high amounts
            if (claim.TotalAmount > 30000) score += 20;
            if (claim.TotalAmount > 50000) score += 30;

            // Lower risk with documentation
            if (!string.IsNullOrEmpty(claim.DocumentPath)) score -= 20;

            // Lower risk with detailed notes
            if (!string.IsNullOrEmpty(claim.AdditionalNotes) && claim.AdditionalNotes.Length > 50)
                score -= 10;

            return Math.Max(0, Math.Min(100, score)); // Clamp between 0-100
        }
    }

    public class ClaimValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Warnings { get; set; }
        public List<string> Errors { get; set; }
        public bool RequiresManagerApproval { get; set; }
        public bool AutoApprovalEligible { get; set; }
        public int RiskScore { get; set; }

        public string RiskLevel
        {
            get
            {
                if (RiskScore >= 70) return "High";
                if (RiskScore >= 40) return "Medium";
                return "Low";
            }
        }

        public string RiskLevelColor
        {
            get
            {
                if (RiskScore >= 70) return "danger";
                if (RiskScore >= 40) return "warning";
                return "success";
            }
        }
    }
}