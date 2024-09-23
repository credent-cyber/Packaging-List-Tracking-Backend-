using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechnoPackaginListTracking.DataContext.Models
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string VendorId { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
