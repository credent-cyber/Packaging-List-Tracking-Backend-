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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Cartons>()
                .HasOne<RequestForm>()
                .WithMany(r => r.Cartons)
                .HasForeignKey(c => c.RequestFormId);

            modelBuilder.Entity<FileUploadDto>()
                .HasOne<RequestForm>()
                .WithMany(r => r.FileUploads)
                .HasForeignKey(f => f.RequestFormId);
        }

        public DbSet<RequestForm> RequestForms { get; set; }
        public DbSet<Cartons> Cartons { get; set; }
        public DbSet<FileUploadDto> FileUploads { get; set; }
        public DbSet<AppSettings> AppSettings { get; set; }
    }
}
