using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TutorsWorldBackend.DAL;
using TutorsWorldBackend.models;
using TutorsWorldBackend.Services;

namespace TutorsWorldBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentController : ControllerBase
    {
        private readonly StudentRegistrationService service;
        public StudentController(StudentRegistrationService _service) 
        {
            service = _service;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] StudentRegistrationVM model)
        {
            var result = await service.Register(model);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(new { message = result.Message });
        }


    }
}
