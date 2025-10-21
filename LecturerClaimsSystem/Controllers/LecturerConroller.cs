using Microsoft.AspNetCore.Mvc;
using LecturerClaimsSystem.Data;
using LecturerClaimsSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LecturerClaimsSystem.Controllers
{
    public class LecturerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public LecturerController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // Check if user is logged in as lecturer
        private bool IsLecturer()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Lecturer";
        }

        public IActionResult Index()
        {
            if (!IsLecturer())
                return RedirectToAction("Login", "Account");

            var username = HttpContext.Session.GetString("FullName");
            var claims = _context.Claims
                .Where(c => c.LecturerName == username)
                .OrderByDescending(c => c.SubmittedDate)
                .ToList();

            ViewBag.Username = username;
            return View(claims);
        }

        [HttpGet]
        public IActionResult SubmitClaim()
        {
            if (!IsLecturer())
                return RedirectToAction("Login", "Account");

            ViewBag.Username = HttpContext.Session.GetString("FullName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitClaim(Claim claim, IFormFile? document)
        {
            if (!IsLecturer())
                return RedirectToAction("Login", "Account");

            try
            {
                claim.LecturerName = HttpContext.Session.GetString("FullName");
                claim.Status = "Pending";
                claim.SubmittedDate = DateTime.Now;

                //file upload
                if (document != null && document.Length > 0)
                {
                    //file size 
                    if (document.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("document", "File size cannot exceed 5MB");
                        return View(claim);
                    }

                    //file type
                    var allowedExtensions = new[] { ".pdf", ".docx", ".xlsx", ".doc", ".xls" };
                    var extension = Path.GetExtension(document.FileName).ToLower();

                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError("document",
                            "Only PDF, Word, and Excel files are allowed");
                        return View(claim);
                    }

                    try
                    {
                        // Ensure uploads folder exists
                        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");

                        // Create directory if it doesn't exist
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        // Generate unique filename
                        var uniqueFileName = Guid.NewGuid().ToString() + extension;
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        // Save file
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await document.CopyToAsync(fileStream);
                        }

                        claim.DocumentPath = "/uploads/" + uniqueFileName;
                        claim.OriginalFileName = document.FileName;
                    }
                    catch (Exception ex)
                    {
                        // Log the error and continue without the document
                        ModelState.AddModelError("document",
                            "Error uploading file: " + ex.Message + ". Claim will be saved without document.");
                        // Don't return - continue to save claim
                    }
                }

                _context.Claims.Add(claim);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Claim submitted successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while submitting the claim: " + ex.Message);
                return View(claim);
            }
        }

        public IActionResult ViewClaim(int id)
        {
            if (!IsLecturer())
                return RedirectToAction("Login", "Account");

            var claim = _context.Claims.Find(id);
            if (claim == null)
                return NotFound();

            return View(claim);
        }
    }
}