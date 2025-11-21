using Microsoft.EntityFrameworkCore;
using LecturerClaimsSystem.Models;

namespace LecturerClaimsSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Claim> Claims { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed initial users
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserId = 1,
                    Username = "lecturer1",
                    Password = "password123", // Use hashing in production!
                    Role = "Lecturer",
                    FullName = "John Smith"
                },
                new User
                {
                    UserId = 2,
                    Username = "coordinator1",
                    Password = "password123",
                    Role = "Coordinator",
                    FullName = "Jane Doe"
                },
                new User
                {
                    UserId = 3,
                    Username = "manager1",
                    Password = "password123",
                    Role = "Manager",
                    FullName = "Bob Johnson"
                },
                new User
                {
                    UserId = 4,
                    Username = "hr1",
                    Password = "password123",
                    Role = "HR",
                    FullName = "Sarah Williams"
                }
            );
        }
    }
}