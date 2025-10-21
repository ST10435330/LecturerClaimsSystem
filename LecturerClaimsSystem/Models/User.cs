using System.ComponentModel.DataAnnotations;

namespace LecturerClaimsSystem.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; } // In production, use hashed passwords!

        [Required]
        public string Role { get; set; } // Lecturer, Coordinator, Manager

        public string FullName { get; set; }
    }
}