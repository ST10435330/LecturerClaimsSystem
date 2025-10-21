using Microsoft.AspNetCore.Mvc;
using LecturerClaimsSystem.Data;
using LecturerClaimsSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LecturerClaimsSystem.Controllers
{
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReviewController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsReviewer()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Coordinator" || role == "Manager";
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
    }
}