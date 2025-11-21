using Microsoft.AspNetCore.Mvc;
using LecturerClaimsSystem.Data;
using LecturerClaimsSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace LecturerClaimsSystem.Controllers
{
    public class HRController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HRController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsHR()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "HR" || role == "Manager";
        }

        public IActionResult Index()
        {
            if (!IsHR())
                return RedirectToAction("Login", "Account");

            ViewBag.Username = HttpContext.Session.GetString("FullName");
            return View();
        }

        public IActionResult Reports()
        {
            if (!IsHR())
                return RedirectToAction("Login", "Account");

            var claims = _context.Claims.ToList();

            // Calculate statistics
            var stats = new
            {
                TotalClaims = claims.Count,
                PendingClaims = claims.Count(c => c.Status == "Pending"),
                ApprovedClaims = claims.Count(c => c.Status == "Approved"),
                RejectedClaims = claims.Count(c => c.Status == "Rejected"),
                TotalApprovedAmount = claims.Where(c => c.Status == "Approved").Sum(c => c.TotalAmount),
                AverageClaimAmount = claims.Any() ? claims.Average(c => c.TotalAmount) : 0,
                HighestClaim = claims.Any() ? claims.Max(c => c.TotalAmount) : 0,
                LowestClaim = claims.Any() ? claims.Min(c => c.TotalAmount) : 0
            };

            ViewBag.Stats = stats;
            ViewBag.Claims = claims;

            return View();
        }

        public IActionResult GenerateInvoice(int id)
        {
            if (!IsHR())
                return RedirectToAction("Login", "Account");

            var claim = _context.Claims.Find(id);
            if (claim == null || claim.Status != "Approved")
                return NotFound();

            return View(claim);
        }

        public IActionResult DownloadInvoice(int id)
        {
            if (!IsHR())
                return RedirectToAction("Login", "Account");

            var claim = _context.Claims.Find(id);
            if (claim == null || claim.Status != "Approved")
                return NotFound();

            var csv = new StringBuilder();
            csv.AppendLine("INVOICE - LECTURER CLAIM PAYMENT");
            csv.AppendLine($"Invoice Date:,{DateTime.Now:yyyy-MM-DD}");
            csv.AppendLine($"Claim ID:,{claim.ClaimId}");
            csv.AppendLine("");
            csv.AppendLine($"Lecturer Name:,{claim.LecturerName}");
            csv.AppendLine($"Hours Worked:,{claim.HoursWorked}");
            csv.AppendLine($"Hourly Rate:,R{claim.HourlyRate:N2}");
            csv.AppendLine($"Total Amount:,R{claim.TotalAmount:N2}");
            csv.AppendLine("");
            csv.AppendLine($"Submitted Date:,{claim.SubmittedDate:yyyy-MM-dd}");
            csv.AppendLine($"Approved Date:,{claim.ReviewedDate:yyyy-MM-dd}");
            csv.AppendLine($"Approved By:,{claim.ReviewedBy}");

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"Invoice_Claim{claim.ClaimId}_{DateTime.Now:yyyyMMdd}.csv");
        }

        public IActionResult MonthlyReport()
        {
            if (!IsHR())
                return RedirectToAction("Login", "Account");

            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            var monthlyClaims = _context.Claims
                .Where(c => c.SubmittedDate.Month == currentMonth && c.SubmittedDate.Year == currentYear)
                .ToList();

            ViewBag.Month = DateTime.Now.ToString("MMMM yyyy");
            ViewBag.Claims = monthlyClaims;

            return View();
        }

        public IActionResult DownloadMonthlyReport()
        {
            if (!IsHR())
                return RedirectToAction("Login", "Account");

            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            var claims = _context.Claims
                .Where(c => c.SubmittedDate.Month == currentMonth && c.SubmittedDate.Year == currentYear)
                .ToList();

            var csv = new StringBuilder();
            csv.AppendLine($"MONTHLY CLAIMS REPORT - {DateTime.Now:MMMM yyyy}");
            csv.AppendLine("");
            csv.AppendLine("Claim ID,Lecturer Name,Hours Worked,Hourly Rate,Total Amount,Status,Submitted Date,Reviewed By");

            foreach (var claim in claims)
            {
                csv.AppendLine($"{claim.ClaimId},{claim.LecturerName},{claim.HoursWorked},{claim.HourlyRate:N2},{claim.TotalAmount:N2},{claim.Status},{claim.SubmittedDate:yyyy-MM-dd},{claim.ReviewedBy ?? "N/A"}");
            }

            csv.AppendLine("");
            csv.AppendLine($"Total Claims:,{claims.Count}");
            csv.AppendLine($"Total Approved Amount:,R{claims.Where(c => c.Status == "Approved").Sum(c => c.TotalAmount):N2}");

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"Monthly_Report_{DateTime.Now:yyyyMM}.csv");
        }

        
        public IActionResult LecturerManagement()
        {
            if (!IsHR())
                return RedirectToAction("Login", "Account");

            try
            {
                var lecturers = _context.Users.Where(u => u.Role == "Lecturer").ToList();

                // If no lecturers, return empty list instead of null
                if (!lecturers.Any())
                {
                    ViewBag.LecturerStats = new List<dynamic>();
                    return View();
                }

                var lecturerStats = lecturers.Select(l =>
                {
                    // Get all claims for this lecturer in memory
                    var lecturerClaims = _context.Claims
                        .Where(c => c.LecturerName == l.FullName)
                        .ToList();

                    return new
                    {
                        Lecturer = l,
                        TotalClaims = lecturerClaims.Count,
                        ApprovedClaims = lecturerClaims.Count(c => c.Status == "Approved"),
                        PendingClaims = lecturerClaims.Count(c => c.Status == "Pending"),
                        RejectedClaims = lecturerClaims.Count(c => c.Status == "Rejected"),
                        TotalEarned = lecturerClaims
                            .Where(c => c.Status == "Approved")
                            .Sum(c => c.TotalAmount)
                    };
                }).ToList();

                ViewBag.LecturerStats = lecturerStats;
                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading lecturer management: " + ex.Message;
                ViewBag.LecturerStats = new List<dynamic>();
                return View();
            }
        }

        public IActionResult LecturerDetails(int id)
        {
            if (!IsHR())
                return RedirectToAction("Login", "Account");

            var lecturer = _context.Users.Find(id);
            if (lecturer == null || lecturer.Role != "Lecturer")
                return NotFound();

            var claims = _context.Claims
                .Where(c => c.LecturerName == lecturer.FullName)
                .OrderByDescending(c => c.SubmittedDate)
                .ToList();

            ViewBag.Lecturer = lecturer;
            ViewBag.Claims = claims;

            var stats = new
            {
                TotalClaims = claims.Count,
                ApprovedClaims = claims.Count(c => c.Status == "Approved"),
                PendingClaims = claims.Count(c => c.Status == "Pending"),
                RejectedClaims = claims.Count(c => c.Status == "Rejected"),
                TotalEarned = claims.Where(c => c.Status == "Approved").Sum(c => c.TotalAmount),
                AverageClaimAmount = claims.Any() ? claims.Average(c => c.TotalAmount) : 0,
                LastClaimDate = claims.Any() ? claims.Max(c => c.SubmittedDate) : (DateTime?)null
            };

            ViewBag.Stats = stats;
            return View();
        }

        [HttpGet]
        public IActionResult EditLecturer(int id)
        {
            if (!IsHR())
                return RedirectToAction("Login", "Account");

            var lecturer = _context.Users.Find(id);
            if (lecturer == null || lecturer.Role != "Lecturer")
                return NotFound();

            return View(lecturer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditLecturer(User lecturer)
        {
            if (!IsHR())
                return RedirectToAction("Login", "Account");

            try
            {
                var existingLecturer = _context.Users.Find(lecturer.UserId);
                if (existingLecturer == null)
                    return NotFound();

                existingLecturer.FullName = lecturer.FullName;
                existingLecturer.Username = lecturer.Username;

                if (!string.IsNullOrEmpty(lecturer.Password))
                {
                    existingLecturer.Password = lecturer.Password;
                }

                _context.SaveChanges();

                TempData["Success"] = "Lecturer information updated successfully!";
                return RedirectToAction("LecturerDetails", new { id = lecturer.UserId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error updating lecturer: " + ex.Message;
                return View(lecturer);
            }
        }

        [HttpGet]
        public IActionResult AddLecturer()
        {
            if (!IsHR())
                return RedirectToAction("Login", "Account");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddLecturer(User lecturer)
        {
            if (!IsHR())
                return RedirectToAction("Login", "Account");

            try
            {
                if (_context.Users.Any(u => u.Username == lecturer.Username))
                {
                    TempData["Error"] = "Username already exists. Please choose a different username.";
                    return View(lecturer);
                }

                lecturer.Role = "Lecturer";
                _context.Users.Add(lecturer);
                _context.SaveChanges();

                TempData["Success"] = "Lecturer added successfully!";
                return RedirectToAction("LecturerManagement");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error adding lecturer: " + ex.Message;
                return View(lecturer);
            }
        }

        public IActionResult DownloadLecturerReport(int id)
        {
            if (!IsHR())
                return RedirectToAction("Login", "Account");

            var lecturer = _context.Users.Find(id);
            if (lecturer == null)
                return NotFound();

            var claims = _context.Claims
                .Where(c => c.LecturerName == lecturer.FullName)
                .OrderByDescending(c => c.SubmittedDate)
                .ToList();

            var csv = new StringBuilder();
            csv.AppendLine($"LECTURER CLAIMS REPORT");
            csv.AppendLine($"Lecturer: {lecturer.FullName}");
            csv.AppendLine($"Username: {lecturer.Username}");
            csv.AppendLine($"Report Generated: {DateTime.Now:yyyy-MM-dd HH:mm}");
            csv.AppendLine("");
            csv.AppendLine("Claim ID,Hours Worked,Hourly Rate,Total Amount,Status,Submitted Date,Reviewed Date,Reviewed By");

            foreach (var claim in claims)
            {
                csv.AppendLine($"{claim.ClaimId},{claim.HoursWorked},{claim.HourlyRate:N2},{claim.TotalAmount:N2},{claim.Status},{claim.SubmittedDate:yyyy-MM-dd},{claim.ReviewedDate?.ToString("yyyy-MM-dd") ?? "N/A"},{claim.ReviewedBy ?? "N/A"}");
            }

            csv.AppendLine("");
            csv.AppendLine($"Total Claims:,{claims.Count}");
            csv.AppendLine($"Approved Claims:,{claims.Count(c => c.Status == "Approved")}");
            csv.AppendLine($"Total Earned:,R{claims.Where(c => c.Status == "Approved").Sum(c => c.TotalAmount):N2}");

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"Lecturer_Report_{lecturer.Username}_{DateTime.Now:yyyyMMdd}.csv");
        }
    }
}