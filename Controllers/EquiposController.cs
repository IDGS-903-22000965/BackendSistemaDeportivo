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

        [HttpGet("{id}/jugadores-detalle")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GetJugadoresDetalle(int id)
        {
            var equipo = await _context.Equipos.FindAsync(id);
            if (equipo == null)
                return NotFound(new { success = false, message = "Equipo no encontrado" });

            var jugadores = await _context.Jugadores
                .Include(j => j.Usuario)
                .Where(j => j.EquipoID == id)
                .Select(j => new
                {
                    j.JugadorID,
                    j.UsuarioID,
                    j.EquipoID,
                    numeroCamiseta = j.NumeroJugador,
                    posicion = j.Posicion,
                    estatus = j.Estatus,
                    fotoURL = j.FotoURL,
                    fechaNacimiento = j.FechaNacimiento,
                    fechaRegistro = j.FechaRegistro,
                    esCapitan = j.Estatus == "Capitan" || equipo.CapitanID == j.UsuarioID,
                    usuario = new
                    {
                        j.Usuario.UsuarioID,
                        j.Usuario.Nombre,
                        j.Usuario.Apellidos,
                        j.Usuario.Email,
                        j.Usuario.Telefono,
                        j.Usuario.FechaRegistro,
                        j.Usuario.Activo
                    }
                })
                .OrderBy(j => j.numeroCamiseta)
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = new
                {
                    equipo = new
                    {
                        equipo.EquipoID,
                        equipo.NombreEquipo,
                        equipo.LogoURL,
                        equipo.CapitanID
                    },
                    jugadores
                }
            });
        }

        [HttpPut("{equipoId}/jugadores/{jugadorId}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> EditarJugador(int equipoId, int jugadorId, [FromBody] EditarJugadorAdminRequest request)
        {
            try
            {
                var jugador = await _context.Jugadores
                    .Include(j => j.Usuario)
                    .FirstOrDefaultAsync(j => j.JugadorID == jugadorId && j.EquipoID == equipoId);

                if (jugador == null)
                    return NotFound(new { success = false, message = "Jugador no encontrado" });

                var usuario = jugador.Usuario;

                if (request.Email != usuario.Email)
                {
                    var emailExiste = await _context.Usuarios.AnyAsync(u => u.Email == request.Email && u.UsuarioID != usuario.UsuarioID);
                    if (emailExiste)
                        return BadRequest(new { success = false, message = "El email ya está en uso" });
                }

                if (request.NumeroCamiseta != jugador.NumeroJugador)
                {
                    var numeroExiste = await _context.Jugadores.AnyAsync(j =>
                        j.EquipoID == equipoId &&
                        j.NumeroJugador == request.NumeroCamiseta &&
                        j.JugadorID != jugadorId);

                    if (numeroExiste)
                        return BadRequest(new { success = false, message = "Ese número de camiseta ya está en uso en el equipo" });
                }

                usuario.Nombre = request.Nombre;
                usuario.Apellidos = request.Apellidos;
                usuario.Email = request.Email;
                usuario.Telefono = request.Telefono;
                usuario.Activo = request.Activo;

                jugador.NumeroJugador = request.NumeroCamiseta;
                jugador.Posicion = request.Posicion;
                jugador.Estatus = request.Estatus;
                jugador.FechaNacimiento = request.FechaNacimiento;
                jugador.FotoURL = request.FotoURL;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Jugador actualizado correctamente",
                    data = new
                    {
                        jugador.JugadorID,
                        jugador.UsuarioID,
                        numeroCamiseta = jugador.NumeroJugador,
                        posicion = jugador.Posicion,
                        estatus = jugador.Estatus,
                        usuario = new
                        {
                            usuario.Nombre,
                            usuario.Apellidos,
                            usuario.Email,
                            usuario.Telefono,
                            usuario.Activo
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error al actualizar: {ex.Message}" });
            }
        }

        [HttpDelete("{equipoId}/jugadores/{jugadorId}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> EliminarJugador(int equipoId, int jugadorId)
        {
            try
            {
                var jugador = await _context.Jugadores
                    .FirstOrDefaultAsync(j => j.JugadorID == jugadorId && j.EquipoID == equipoId);

                if (jugador == null)
                    return NotFound(new { success = false, message = "Jugador no encontrado" });

                var equipo = await _context.Equipos.FindAsync(equipoId);

                if (equipo.CapitanID == jugador.UsuarioID)
                    return BadRequest(new { success = false, message = "No se puede eliminar al capitán del equipo" });

                var tieneEventos = await _context.EventosPartido.AnyAsync(e => e.JugadorID == jugadorId);
                if (tieneEventos)
                {
                    jugador.Estatus = "Inactivo";
                    await _context.SaveChangesAsync();
                    return Ok(new { success = true, message = "Jugador desactivado correctamente (tiene historial de eventos)" });
                }

                _context.Jugadores.Remove(jugador);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Jugador eliminado correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error al eliminar: {ex.Message}" });
            }
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

    public class EditarJugadorAdminRequest
    {
        public string Nombre { get; set; }
        public string Apellidos { get; set; }
        public string Email { get; set; }
        public string? Telefono { get; set; }
        public int NumeroCamiseta { get; set; }
        public string Posicion { get; set; }
        public string Estatus { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string? FotoURL { get; set; }
        public bool Activo { get; set; }
    }
}