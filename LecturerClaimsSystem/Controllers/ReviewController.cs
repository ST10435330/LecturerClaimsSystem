using Microsoft.AspNetCore.Mvc;
using LecturerClaimsSystem.Data;
using LecturerClaimsSystem.Models;
using LecturerClaimsSystem.Services;
using Microsoft.EntityFrameworkCore;

namespace LecturerClaimsSystem.Controllers
{
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ClaimValidationService _validationService;

        public ReviewController(ApplicationDbContext context, ClaimValidationService validationService)
        {
            _context = context;
            _validationService = validationService;
        }

        private bool IsReviewer()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Coordinator" || role == "Manager" || role == "HR";
        }

        public IActionResult Index()
        {
            if (!IsReviewer())
                return RedirectToAction("Login", "Account");

            var claims = _context.Claims
                .OrderByDescending(c => c.SubmittedDate)
                .ToList();

            ViewBag.Username = HttpContext.Session.GetString("FullName");
            ViewBag.Role = HttpContext.Session.GetString("Role");
            return View(claims);
        }

        public IActionResult ViewClaim(int id)
        {
            if (!IsReviewer())
                return RedirectToAction("Login", "Account");

            var claim = _context.Claims.Find(id);
            if (claim == null)
                return NotFound();

            // Perform automated validation
            var validationResult = _validationService.ValidateClaim(claim);
            ViewBag.ValidationResult = validationResult;
            ViewBag.Role = HttpContext.Session.GetString("Role");

            return View(claim);
        }

        [HttpPost]
        public IActionResult ApproveClaim(int id)
        {
            if (!IsReviewer())
                return RedirectToAction("Login", "Account");

            try
            {
                var claim = _context.Claims.Find(id);
                if (claim == null)
                    return NotFound();

                // Validate before approval
                var validationResult = _validationService.ValidateClaim(claim);

                var userRole = HttpContext.Session.GetString("Role");

                // Check if manager approval is required
                if (validationResult.RequiresManagerApproval && userRole != "Manager" && userRole != "HR")
                {
                    TempData["Error"] = "This claim requires Manager approval due to: " +
                                       string.Join(", ", validationResult.Warnings);
                    return RedirectToAction("ViewClaim", new { id = id });
                }

                claim.Status = "Approved";
                claim.ReviewedDate = DateTime.Now;
                claim.ReviewedBy = HttpContext.Session.GetString("FullName");

                _context.SaveChanges();

                TempData["Success"] = "Claim approved successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error approving claim: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public IActionResult RejectClaim(int id, string rejectionReason)
        {
            if (!IsReviewer())
                return RedirectToAction("Login", "Account");

            try
            {
                var claim = _context.Claims.Find(id);
                if (claim == null)
                    return NotFound();

                claim.Status = "Rejected";
                claim.ReviewedDate = DateTime.Now;
                claim.ReviewedBy = HttpContext.Session.GetString("FullName");
                claim.RejectionReason = rejectionReason;

                _context.SaveChanges();

                TempData["Success"] = "Claim rejected!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error rejecting claim: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public IActionResult AutoApproveClaim(int id)
        {
            if (!IsReviewer())
                return RedirectToAction("Login", "Account");

            try
            {
                var claim = _context.Claims.Find(id);
                if (claim == null)
                    return NotFound();

                var validationResult = _validationService.ValidateClaim(claim);

                if (validationResult.AutoApprovalEligible)
                {
                    claim.Status = "Approved";
                    claim.ReviewedDate = DateTime.Now;
                    claim.ReviewedBy = "System (Auto-Approved)";

                    _context.SaveChanges();

                    TempData["Success"] = "Claim auto-approved successfully!";
                }
                else
                {
                    TempData["Error"] = "Claim does not meet auto-approval criteria";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error auto-approving claim: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
    }
}