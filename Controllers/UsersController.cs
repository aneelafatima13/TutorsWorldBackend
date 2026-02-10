using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TutorsWorldBackend.DAL;
using TutorsWorldBackend.models;


namespace TutorsWorldBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UsersDAL _usersDAL;
        private readonly IConfiguration _config; // 1. Declare the field

        public UsersController(IConfiguration config)
        {
            _config = config; // 2. Assign the field so other methods can use it

            string cs = _config.GetConnectionString("DefaultConnection");
            _usersDAL = new UsersDAL(cs);
        }

        private string GenerateJwtToken(LoginUser user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Claims are the "data" stored inside the token
            var claims = new[] {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.UserName),
        new Claim(ClaimTypes.Role, user.Role), // "Tutor", "Student", etc.
        new Claim("TutorId", user.TutorId?.ToString() ?? ""),
        new Claim("StudentId", user.StudentId?.ToString() ?? "")
    };

            var token = new JwtSecurityToken(
                _config["Jwt:Issuer"],
                _config["Jwt:Audience"],
                claims,
                expires: DateTime.Now.AddDays(2),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            bool isUserFound = await _usersDAL.UsernameExistsAsync(model.Username);
            if (isUserFound)
            {
                LoginUser loginUser = await _usersDAL.GetUserDatabyusername(model.Username);
                if (loginUser.Password != model.Password) // Note: In production, use Hashing!
                    return Unauthorized(new { message = "Incorrect password" });

                var token = GenerateJwtToken(loginUser);

                return Ok(new
                {
                    success = true,
                    token = token, // Send this to the frontend
                    userdata = loginUser
                });
            }
            else
            {
                return NotFound(new { message = "Username not found" });
            }

           
           
            
        }

        [Authorize]
        [HttpGet("GetTutorDetails/{id}")]
        public async Task<IActionResult> GetTutorDetails(long id)
        {
            try
            {
                // Simply call the DAL method
                var tutor = await _usersDAL.GetTutorFullDetailsAsync(id);

                if (tutor == null)
                {
                    return NotFound(new { success = false, message = "Tutor not found" });
                }

                return Ok(new { success = true, data = tutor });
            }
            catch (Exception ex)
            {
                // Log ex.Message here
                return StatusCode(500, new { success = false, message = "An error occurred while fetching tutor details." });
            }
        }

        [Authorize]
        [HttpGet("GetStudentDetails/{id}")]
        public async Task<IActionResult> GetStudentDetails(long id)
        {
            var data = await _usersDAL.GetStudentDataByIdAsync(id);
            return data != null ? Ok(new { success = true, data = data }) : NotFound();
        }

        [Authorize]
        [HttpGet("GetGuardianDetails/{id}")]
        public async Task<IActionResult> GetGuardianDetails(long id)
        {
            var data = await _usersDAL.GetGuardianDataByIdAsync(id);
            return data != null ? Ok(data) : NotFound();
        }

        [Authorize]
        [HttpPost("HireTutor")]
        public async Task<IActionResult> HireTutor([FromBody] HireRequest request)
        {
            try
            {
                // 1. Basic Validation
                if (request.StudentId <= 0 || request.TutorId <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid Student or Tutor ID." });
                }

                // 2. Ensure at least one hiring party is identified
                if (request.GuardianId == null && request.HiredByStudentId == null)
                {
                    return BadRequest(new { success = false, message = "Hiring party (Guardian or Student) must be specified." });
                }

                // 3. Call DAL
                long newHireId = await _usersDAL.HireTutorAsync(
                    request.StudentId,
                    request.TutorId,
                    request.GuardianId,
                    request.HiredByStudentId
                );

                if (newHireId > 0)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Tutor hired successfully!",
                        hireId = newHireId
                    });
                }

                return BadRequest(new { success = false, message = "Could not complete the hiring process." });
            }
            catch (Exception ex)
            {
                // Log the error here
                return StatusCode(500, new { success = false, message = "Internal server error: " + ex.Message });
            }
        }

        [Authorize]
        [HttpGet("GetConnections/{id}/{role}")]
        public async Task<IActionResult> GetConnections(long id, string role)
        {
            try
            {
                var connections = await _usersDAL.GetConnectionsAsync(id, role);
                return Ok(new { success = true, data = connections });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        public class LoginRequest { public string Username { get; set; } public string Password { get; set; } }
    }
}
