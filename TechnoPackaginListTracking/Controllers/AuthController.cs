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
using TechnoPackaginListTracking.Dto.Auth;

namespace TechnoPackaginListTracking.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IConfiguration configuration,
            IEmailSender emailSender,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _emailSender = emailSender;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterParameters model)
        {
            try
            {
                var user = new IdentityUser { UserName = model.UserName, Email = model.Email, PhoneNumber = model.MobileNo };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Directly set the email confirmation flag to true
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);

                    // Assign the first user as SuperAdmin
                    var users = await _userManager.Users.ToListAsync();
                    if (users.Count == 1)
                    {
                        await _userManager.AddToRoleAsync(user, "SuperAdmin");
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(user, "Vendor");
                    }

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

                var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Email, user.Email)
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

                return Ok(new { Token = tokenString });
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

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetLink = Url.Action("resetpassword", "Auth", new { token, email = model.Email }, Request.Scheme);

                await _emailSender.SendEmailAsync(user.Email, "Reset Password", $"Please reset your password by clicking <a href='{resetLink}'>here</a>.");

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
    }
}
