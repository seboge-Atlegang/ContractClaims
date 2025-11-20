using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ContractClaims.Models;
using System.Security.Claims;

namespace ContractClaims.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> opts) : base(opts) { }

        public DbSet<LecturerClaim> LecturerClaims { get; set; }

        
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Store enum as string
            builder.Entity<LecturerClaim>()
                .Property(c => c.Status)
                .HasConversion<string>();
        }
    }
}
