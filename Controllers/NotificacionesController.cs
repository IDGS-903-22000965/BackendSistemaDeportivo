using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TorneoManager.API.Data;
using TorneoManager.API.Models;
using System.Security.Claims;

namespace TorneoManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificacionesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotificacionesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotificaciones()
        {
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var notificaciones = await _context.Notificaciones
                .Where(n => n.UsuarioID == usuarioId)
                .OrderByDescending(n => n.FechaCreacion)
                .ToListAsync();
            return Ok(notificaciones);
        }

        [HttpPut("{id}/leer")]
        public async Task<IActionResult> MarcarLeida(int id)
        {
            var notificacion = await _context.Notificaciones.FindAsync(id);
            if (notificacion == null) return NotFound();

            notificacion.Leida = true;
            await _context.SaveChangesAsync();
            return Ok(notificacion);
        }

        [HttpPut("leer-todas")]
        public async Task<IActionResult> MarcarTodasLeidas()
        {
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var notificaciones = await _context.Notificaciones
                .Where(n => n.UsuarioID == usuarioId && !n.Leida)
                .ToListAsync();

            foreach (var n in notificaciones)
                n.Leida = true;

            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Todas las notificaciones marcadas como leídas" });
        }
    }
}