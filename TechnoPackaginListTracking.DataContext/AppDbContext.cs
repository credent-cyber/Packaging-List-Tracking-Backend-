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

        public DbSet<Details> Details { get; set; }
        public DbSet<RequestForm> RequestForm { get; set; }
        public DbSet<Cartons> Cartons { get; set; }
        public DbSet<FileUploadDto> FileUploads { get; set; }
    }
}
