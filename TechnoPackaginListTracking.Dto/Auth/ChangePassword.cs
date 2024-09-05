using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechnoPackaginListTracking.Dto.Auth
{
    public class ChangePassword
    {


        [Required]
        //[DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        //[DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        //[DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
