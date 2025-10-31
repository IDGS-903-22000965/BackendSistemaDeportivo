using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TorneoManager.API.Data;
using TorneoManager.API.Models;

namespace TorneoManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class JugadoresController : ControllerBase
    {
        private readonly AppDbContext _context;

        public JugadoresController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetJugador(int id)
        {
            var jugador = await _context.Jugadores.FindAsync(id);
            if (jugador == null) return NotFound();
            return Ok(jugador);
        }

        [HttpPost]
        public async Task<IActionResult> CreateJugador([FromBody] Jugador jugador)
        {
            // Crear usuario para el jugador
            var usuario = new Usuario
            {
                Email = $"jugador{DateTime.Now.Ticks}@temp.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                Nombre = "Temporal",
                Apellidos = "Temporal"
            };
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            jugador.UsuarioID = usuario.UsuarioID;
            _context.Jugadores.Add(jugador);
            await _context.SaveChangesAsync();

            var rolJugador = await _context.Roles.FirstAsync(r => r.NombreRol == "Jugador");
            _context.UsuarioRoles.Add(new UsuarioRol { UsuarioID = usuario.UsuarioID, RolID = rolJugador.RolID });
            await _context.SaveChangesAsync();

            return Ok(jugador);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateJugador(int id, [FromBody] Jugador jugador)
        {
            var jugadorExistente = await _context.Jugadores.FindAsync(id);
            if (jugadorExistente == null) return NotFound();

            jugadorExistente.NumeroJugador = jugador.NumeroJugador;
            jugadorExistente.Posicion = jugador.Posicion;
            jugadorExistente.FotoURL = jugador.FotoURL;
            jugadorExistente.FechaNacimiento = jugador.FechaNacimiento;

            await _context.SaveChangesAsync();
            return Ok(jugadorExistente);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteJugador(int id)
        {
            var jugador = await _context.Jugadores.FindAsync(id);
            if (jugador == null) return NotFound();

            _context.Jugadores.Remove(jugador);
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Jugador eliminado" });
        }

        [HttpPost("{id}/documentos")]
        public async Task<IActionResult> SubirDocumento(int id, [FromBody] DocumentoJugador documento)
        {
            documento.JugadorID = id;
            _context.DocumentosJugador.Add(documento);
            await _context.SaveChangesAsync();
            return Ok(documento);
        }

        [HttpGet("{id}/documentos")]
        public async Task<IActionResult> GetDocumentos(int id)
        {
            var documentos = await _context.DocumentosJugador.Where(d => d.JugadorID == id).ToListAsync();
            return Ok(documentos);
        }

        [HttpGet("{id}/sanciones")]
        public async Task<IActionResult> GetSanciones(int id)
        {
            var sanciones = await _context.Sanciones.Where(s => s.JugadorID == id && s.Activa).ToListAsync();
            return Ok(sanciones);
        }
    }
}