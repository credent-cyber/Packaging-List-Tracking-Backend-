using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechnoPackaginListTracking.Dto;

namespace TechnoPackaginListTracking.DataContext
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options, bool ensureCreated = true) : base(options)
        {
            if (ensureCreated)
                Database.EnsureCreated();
        }

       
        public DbSet<RequestForm> RequestForms { get; set; }
        public DbSet<Cartons> Cartons { get; set; }
        public DbSet<FileUploads> FileUploads { get; set; }
        public DbSet<AppSettings> AppSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<RequestForm>()
            //    .HasMany(r => r.Cartons)
            //    //.WithOne(c => c.RequestForm)
            //    .HasForeignKey(c => c.RequestFormId);

            //modelBuilder.Entity<RequestForm>()
            //    .HasMany(r => r.FileUploads)
            //   // .WithOne(f => f.RequestForm)
            //    .HasForeignKey(f => f.RequestFormId);
        }
    }
}
