using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TorneoManager.API.Data;
using TorneoManager.API.Models;

namespace TorneoManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsuarioController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsuarioController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/usuario/perfil
        [HttpGet("perfil")]
        public async Task<IActionResult> ObtenerPerfil()
        {
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var usuario = await _context.Usuarios.FindAsync(usuarioId);

            if (usuario == null)
                return NotFound(new { success = false, message = "Usuario no encontrado" });

            return Ok(new
            {
                success = true,
                data = new
                {
                    usuario.UsuarioID,
                    usuario.Email,
                    usuario.Nombre,
                    usuario.Apellidos,
                    usuario.Telefono,
                    usuario.FechaRegistro,
                    usuario.Activo
                }
            });
        }

        // PUT: api/usuario/actualizar
        [HttpPut("actualizar")]
        public async Task<IActionResult> ActualizarUsuario([FromBody] ActualizarUsuarioRequest request)
        {
            try
            {
                var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var usuario = await _context.Usuarios.FindAsync(usuarioId);

                if (usuario == null)
                    return NotFound(new { success = false, message = "Usuario no encontrado" });

                // Verificar si el email ya existe en otro usuario
                if (request.Email != usuario.Email)
                {
                    var emailExiste = await _context.Usuarios.AnyAsync(u => u.Email == request.Email && u.UsuarioID != usuarioId);
                    if (emailExiste)
                        return BadRequest(new { success = false, message = "El email ya está en uso" });
                }

                // Actualizar solo los campos permitidos
                usuario.Nombre = request.Nombre;
                usuario.Email = request.Email;
                usuario.Telefono = request.Telefono;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Usuario actualizado correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error al actualizar: {ex.Message}" });
            }
        }

        // PUT: api/usuario/cambiar-password
        [HttpPut("cambiar-password")]
        public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordRequest request)
        {
            try
            {
                var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var usuario = await _context.Usuarios.FindAsync(usuarioId);

                if (usuario == null)
                    return NotFound(new { success = false, message = "Usuario no encontrado" });

                // Verificar contraseña actual
                if (!BCrypt.Net.BCrypt.Verify(request.PasswordActual, usuario.PasswordHash))
                    return BadRequest(new { success = false, message = "Contraseña actual incorrecta" });

                // Actualizar contraseña
                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.PasswordNuevo);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Contraseña actualizada correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error al cambiar contraseña: {ex.Message}" });
            }
        }
    }

    public class ActualizarUsuarioRequest
    {
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string? Telefono { get; set; }
    }

    public class CambiarPasswordRequest
    {
        public string PasswordActual { get; set; }
        public string PasswordNuevo { get; set; }
    }
}