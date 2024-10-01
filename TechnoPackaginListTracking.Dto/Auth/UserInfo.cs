using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechnoPackaginListTracking.Dto.Auth
{
    public class UserInfo
    {
        public bool IsAuthenticated { get; set; }
        public string UserName { get; set; }
        public Dictionary<string, string> ExposedClaims { get; set; }

        public List<string> Roles { get; set; }

        public string Email { get; set; }
        public string VendorId { get; set; }
        public string VendorName { get; set; }
        public string Role { get; set; }
        public string MobileNo { get; set; }
        public bool IsActive { get; set; }
    }
}
