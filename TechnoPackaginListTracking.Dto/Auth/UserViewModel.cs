using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechnoPackaginListTracking.Dto.Auth
{
    public class UserViewModel
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string VendorId { get; set; }
        public string VendorName { get; set; }
        public string MobileNo { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
    }
}
