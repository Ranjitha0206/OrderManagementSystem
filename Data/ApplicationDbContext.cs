// This provides Identity-related database functionality
// (Users, Roles, Login tables etc.)
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

// This is needed for working with Entity Framework Core
using Microsoft.EntityFrameworkCore;
using OrderManagementSystem.Models;
// Namespace of your project
namespace OrderManagementSystem.Data
{
    // ApplicationDbContext is your main database class
    // IdentityDbContext automatically includes:
    // - AspNetUsers table
    // - AspNetRoles table
    // - AspNetUserRoles table
    // - Other authentication tables
    public class ApplicationDbContext : IdentityDbContext
    {
        // This constructor receives database configuration
        // from Program.cs and passes it to the base class
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Later we will add our own tables here like:
        public DbSet<Order> Orders { get; set; }

    }
}
