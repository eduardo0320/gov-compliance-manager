using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/roles")]
    public class RolesController : ControllerBase
    {
        private readonly IRolService _rolService;

        public RolesController(IRolService rolService)
        {
            _rolService = rolService;
        }

        [HttpGet]
        [Authorize] // Igual que tus otros endpoints
        public async Task<IActionResult> GetRoles()
        {
            var rolActual = User.Claims.FirstOrDefault(c => c.Type == "rol")?.Value;
            if (rolActual != "ADMIN" && rolActual != "SUPERADMIN") return Forbid();

            try
            {
                var roles = await _rolService.ObtenerTodosLosRoles();
                return Ok(roles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }
    }
}
