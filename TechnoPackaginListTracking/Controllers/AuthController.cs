using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TechnoPackaginListTracking.DataContext.Models;
using TechnoPackaginListTracking.Dto.Auth;

namespace TechnoPackaginListTracking.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IConfiguration configuration,
            IEmailSender emailSender,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _emailSender = emailSender;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterParameters model)
        {
            try
            {
                // Retrieve the list of users
                var userCount = await _userManager.Users.CountAsync();
                if (userCount > 0) {
                    return null; 
                }
                var user = new ApplicationUser
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    PhoneNumber = model.MobileNo,
                    VendorId = model.VendorId,
                    VendorName = model.VendorName,
                    IsActive = model.IsActive
                };

                // Attempt to create the user
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Directly set the email confirmation flag to true
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);

                    string roleToAssign;

                    if (userCount == 1)
                    {
                        // Assign "SuperAdmin" role to the first user
                        roleToAssign = "SuperAdmin";
                    }
                    else
                    {
                        // Assign "User" role to subsequent users
                        roleToAssign = "Vendor";
                    }

                    // Ensure the role exists before assigning it
                    if (!await _roleManager.RoleExistsAsync(roleToAssign))
                    {
                        var roleResult = await _roleManager.CreateAsync(new IdentityRole<Guid>(roleToAssign));
                        if (!roleResult.Succeeded)
                        {
                            _logger.LogError($"Error creating role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to create role.");
                        }
                    }

                    // Assign the role to the user
                    await _userManager.AddToRoleAsync(user, roleToAssign);

                    _logger.LogInformation($"User registered successfully: {model.Email}");

                    return Ok("User registered successfully.");
                }

                _logger.LogWarning($"User registration failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                return BadRequest(result.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while registering the user.");
            }
        }

        [HttpPost("vendor-register")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> VendorRegister([FromBody] RegisterParameters model)
        {
            try
            {
                var user = new ApplicationUser
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    PhoneNumber = model.MobileNo,
                    VendorId = model.VendorId,
                    VendorName = model.VendorName,
                    IsActive = model.IsActive
                };

                // Attempt to create the user
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Directly set the email confirmation flag to true
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);

                    // Retrieve the list of users
                    var userCount = await _userManager.Users.CountAsync();

                    string roleToAssign;

                    if (userCount == 1)
                    {
                        // Assign "SuperAdmin" role to the first user
                        roleToAssign = "SuperAdmin";
                    }
                    else
                    {
                        // Assign "User" role to subsequent users
                        roleToAssign = "Vendor";
                    }

                    // Ensure the role exists before assigning it
                    if (!await _roleManager.RoleExistsAsync(roleToAssign))
                    {
                        var roleResult = await _roleManager.CreateAsync(new IdentityRole<Guid>(roleToAssign));
                        if (!roleResult.Succeeded)
                        {
                            _logger.LogError($"Error creating role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to create role.");
                        }
                    }

                    // Assign the role to the user
                    await _userManager.AddToRoleAsync(user, roleToAssign);

                    _logger.LogInformation($"User registered successfully: {model.Email}");

                    return Ok("User registered successfully.");
                }

                _logger.LogWarning($"User registration failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                return BadRequest(result.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while registering the user.");
            }
        }



        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginParameters model)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(model.UserEmail);
                if (user == null || !(await _userManager.CheckPasswordAsync(user, model.Password)))
                {
                    _logger.LogWarning("Invalid login attempt");
                    return Unauthorized("Invalid login attempt");
                }
                // Retrieve roles for the user
                var roles = await _userManager.GetRolesAsync(user);
                var roleName = roles.FirstOrDefault();

                var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role,roleName)
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Issuer"],
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(30),
                    signingCredentials: creds);

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                _logger.LogInformation($"User logged in successfully: {model.UserEmail}");

                return Ok(new { Token = tokenString, Role = roleName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while logging in.");
            }
        }


        [HttpPost("forgetpassword")]
        public async Task<IActionResult> ForgetPassword([FromBody] ForgetPassword model)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    _logger.LogWarning($"Forget password request failed for {model.Email}: User not found or email not confirmed");
                    return BadRequest("User not found or email not confirmed");
                }

                // Generate password reset token
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                // Read ClientAppBaseUrl from configuration
                var clientAppBaseUrl = _configuration["ClientAppBaseUrl"];

                // Construct reset password link with the ReactJS app URL
                var resetLink = $"{clientAppBaseUrl}/Resetpassword?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(model.Email)}";

                // Send email with reset password link
                await _emailSender.SendEmailAsync(user.Email, "Reset Password",
                    $"Please reset your password by clicking <a href='{resetLink}'>here</a>.");

                _logger.LogInformation($"Password reset email sent to: {model.Email}");

                return Ok("Password reset email sent.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset request");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while sending the password reset email.");
            }
        }


        [HttpPost("resetpassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest model)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    _logger.LogWarning($"Reset password failed for {model.Email}: User not found");
                    return BadRequest("User not found");
                }

                var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
                if (result.Succeeded)
                {
                    _logger.LogInformation($"Password reset successfully for: {model.Email}");
                    return Ok("Password reset successfully");
                }

                _logger.LogWarning($"Password reset failed for {model.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                return BadRequest(result.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while resetting the password.");
            }
        }

        [Authorize]
        [HttpPost("ChangePassword")]
        public async Task<IActionResult> ChangePassword(ChangePassword changePassword)
        {
            try
            {
                var user = await _userManager.GetUserAsync(HttpContext.User);

                if (user == null)
                {
                    return BadRequest("User not found");
                }

                // Check if the current password is correct
                var passwordCheck = await _userManager.CheckPasswordAsync(user, changePassword.CurrentPassword);
                if (!passwordCheck)
                {
                    return BadRequest("Invalid current password");
                }

                // Change the user's password
                var changePasswordResult = await _userManager.ChangePasswordAsync(user, changePassword.CurrentPassword, changePassword.NewPassword);

                if (changePasswordResult.Succeeded)
                {
                    return Ok("Password changed successfully");
                }
                else
                {
                    return BadRequest("Failed to change password");
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it accordingly
                return BadRequest($"Failed to change password: {ex.Message}");
            }
        }

        [Authorize]
        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok();
        }

        [Authorize]
        [HttpGet("UserCount")]
        public async Task<IActionResult> UserCount()
        {
            var userCount = await _userManager.Users.CountAsync();
            return Ok(new { Count = userCount });
        }

        [HttpPost("UpdateUserRole")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> UpdateUserRole(UserViewModel userViewModel)
        {
            var user = await _userManager.FindByIdAsync(userViewModel.Id.ToString());

            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            // Remove all roles from the user
            var removeRolesResult = await _userManager.RemoveFromRolesAsync(user, roles);
            if (!removeRolesResult.Succeeded)
            {
                return BadRequest(removeRolesResult.Errors.FirstOrDefault()?.Description);
            }

            // Add the new role to the user
            var role = await _roleManager.FindByNameAsync(userViewModel.Role);
            if (role == null)
            {
                role = new IdentityRole<Guid>(userViewModel.Role);
                var createRoleResult = await _roleManager.CreateAsync(role);
                if (!createRoleResult.Succeeded)
                {
                    return BadRequest(createRoleResult.Errors.FirstOrDefault()?.Description);
                }
            }

            var addRoleResult = await _userManager.AddToRoleAsync(user, userViewModel.Role);
            if (!addRoleResult.Succeeded)
            {
                return BadRequest(addRoleResult.Errors.FirstOrDefault()?.Description);
            }

            return Ok();
        }

        [HttpGet("GetUsers")]
        [Authorize]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userManager.Users.ToListAsync();

            var userViewModels = users.Select(u => new UserViewModel
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                VendorId = u.VendorId,
                VendorName = u.VendorName,
                Role = _userManager.GetRolesAsync(u).Result.FirstOrDefault(),
                IsActive = u.IsActive,
            }).ToList();

            return Ok(userViewModels);
        }

        [HttpGet("MyInfo")]
        [Authorize]
        public async Task<IActionResult> MyInfo()
        {
            var userEmail = HttpContext.User.Claims.ToList()[2].Value;
            var user = await _userManager.FindByEmailAsync(userEmail);

            if (user == null)
            {
                return BadRequest("User not found");
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            var userInfo = new UserInfo
            {
                IsAuthenticated = User.Identity.IsAuthenticated,
                UserName = user.UserName,
                ExposedClaims = User.Claims.ToDictionary(c => c.Type, c => c.Value),
                Roles = userRoles.ToList(),
                Email = user.Email,
                VendorId = user.VendorId,
                VendorName = user.VendorName,
                MobileNo = user.PhoneNumber
            };

            return Ok(userInfo);
        }
    }
}
