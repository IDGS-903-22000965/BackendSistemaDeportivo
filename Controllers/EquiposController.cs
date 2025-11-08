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
    public class EquiposController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EquiposController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetEquipos()
        {
            var equipos = await _context.Equipos.Where(e => e.Activo).ToListAsync();
            return Ok(equipos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetEquipo(int id)
        {
            var equipo = await _context.Equipos.FindAsync(id);
            if (equipo == null) return NotFound();
            return Ok(equipo);
        }

        [HttpGet("mis-equipos")]
        public async Task<IActionResult> GetMisEquipos()
        {
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var equipos = await _context.Equipos.Where(e => e.CapitanID == usuarioId).ToListAsync();
            return Ok(equipos);
        }

        [HttpGet("{id}/jugadores")]
        public async Task<IActionResult> GetJugadores(int id)
        {
            var jugadores = await _context.Jugadores
                .Where(j => j.EquipoID == id)
                .Join(_context.Usuarios, j => j.UsuarioID, u => u.UsuarioID, (j, u) => new
                {
                    j.JugadorID,
                    j.NumeroJugador,
                    j.Posicion,
                    j.Estatus,
                    Nombre = u.Nombre + " " + u.Apellidos,
                    j.FotoURL
                })
                .ToListAsync();
            return Ok(jugadores);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEquipo(int id, [FromBody] Equipo equipo)
        {
            var equipoExistente = await _context.Equipos.FindAsync(id);
            if (equipoExistente == null) return NotFound();

            equipoExistente.NombreEquipo = equipo.NombreEquipo;
            equipoExistente.ColorUniformePrimario = equipo.ColorUniformePrimario;
            equipoExistente.ColorUniformeSecundario = equipo.ColorUniformeSecundario;
            equipoExistente.LogoURL = equipo.LogoURL;

            await _context.SaveChangesAsync();
            return Ok(equipoExistente);
        }
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CreateEquipo([FromBody] Equipo equipo)
        {
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            equipo.CapitanID = usuarioId;

            _context.Equipos.Add(equipo);
            await _context.SaveChangesAsync();
            return Ok(equipo);
        }
    }
}