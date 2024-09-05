

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace TechnoPackaginListTracking.DataContext
{
    public class AuthDbContext : IdentityDbContext<IdentityUser>
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Seed roles (SuperAdmin, Admin, Vendor)
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole { Name = "SuperAdmin", NormalizedName = "SUPERADMIN" },
                new IdentityRole { Name = "Admin", NormalizedName = "ADMIN" },
                new IdentityRole { Name = "Vendor", NormalizedName = "VENDOR" }
            );
        }
    }
}
