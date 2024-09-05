using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechnoPackaginListTracking.Dto.Auth
{

    public class LoginParameters
    {

        [Required]
        [EmailAddress]
        public string UserEmail { get; set; }

        [Required]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }

}
