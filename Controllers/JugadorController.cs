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
    public class JugadorController : ControllerBase
    {
        private readonly AppDbContext _context;

        public JugadorController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/jugador/mi-perfil
        [HttpGet("mi-perfil")]
        public async Task<IActionResult> ObtenerMiPerfil()
        {
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var jugador = await _context.Jugadores
                .Include(j => j.Usuario)
                .Include(j => j.Equipo)
                .FirstOrDefaultAsync(j => j.UsuarioID == usuarioId);

            if (jugador == null)
                return NotFound(new { success = false, message = "Jugador no encontrado" });

            return Ok(new
            {
                success = true,
                data = new
                {
                    jugador.JugadorID,
                    jugador.UsuarioID,
                    jugador.EquipoID,
                    numeroCamiseta = jugador.NumeroJugador,
                    posicion = jugador.Posicion,
                    esCapitan = jugador.Estatus == "Capitan",
                    fechaIngreso = jugador.FechaRegistro,
                    activo = jugador.Estatus == "Activo",
                    usuario = new
                    {
                        jugador.Usuario.UsuarioID,
                        jugador.Usuario.Nombre,
                        jugador.Usuario.Email,
                        jugador.Usuario.Telefono
                    },
                    equipo = new
                    {
                        jugador.Equipo.EquipoID,
                        nombreEquipo = jugador.Equipo.NombreEquipo,
                        logo = jugador.Equipo.LogoURL
                    }
                }
            });
        }

        // GET: api/jugador/estadisticas
        [HttpGet("estadisticas")]
        public async Task<IActionResult> ObtenerEstadisticas()
        {
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var jugador = await _context.Jugadores.FirstOrDefaultAsync(j => j.UsuarioID == usuarioId);

            if (jugador == null)
                return NotFound(new { success = false, message = "Jugador no encontrado" });

            // Obtener partidos jugados
            var partidosJugados = await _context.Partidos
                .Where(p => (p.EquipoLocalID == jugador.EquipoID || p.EquipoVisitanteID == jugador.EquipoID)
                    && p.Estatus == "Finalizado")
                .CountAsync();

            // Obtener goles
            var goles = await _context.EventosPartido
                .Where(e => e.JugadorID == jugador.JugadorID && e.TipoEvento == "Gol")
                .CountAsync();

            // Obtener asistencias
            var asistencias = await _context.EventosPartido
                .Where(e => e.AsistenciaJugadorID == jugador.JugadorID)
                .CountAsync();

            // Obtener tarjetas amarillas
            var tarjetasAmarillas = await _context.EventosPartido
                .Where(e => e.JugadorID == jugador.JugadorID && e.TipoEvento == "TarjetaAmarilla")
                .CountAsync();

            // Obtener tarjetas rojas
            var tarjetasRojas = await _context.EventosPartido
                .Where(e => e.JugadorID == jugador.JugadorID && e.TipoEvento == "TarjetaRoja")
                .CountAsync();

            var promedioGoles = partidosJugados > 0 ? (double)goles / partidosJugados : 0;

            return Ok(new
            {
                success = true,
                data = new
                {
                    jugadorID = jugador.JugadorID,
                    partidosJugados,
                    goles,
                    asistencias,
                    tarjetasAmarillas,
                    tarjetasRojas,
                    promedioGoles = Math.Round(promedioGoles, 2)
                }
            });
        }

        // GET: api/jugador/partidos
        [HttpGet("partidos")]
        public async Task<IActionResult> ObtenerPartidos()
        {
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var jugador = await _context.Jugadores.FirstOrDefaultAsync(j => j.UsuarioID == usuarioId);

            if (jugador == null)
                return NotFound(new { success = false, message = "Jugador no encontrado" });

            var partidos = await _context.Partidos
                .Include(p => p.EquipoLocal)
                .Include(p => p.EquipoVisitante)
                .Where(p => p.EquipoLocalID == jugador.EquipoID || p.EquipoVisitanteID == jugador.EquipoID)
                .OrderByDescending(p => p.FechaHora)
                .Select(p => new
                {
                    p.PartidoID,
                    p.EquipoLocalID,
                    p.EquipoVisitanteID,
                    fechaPartido = p.FechaHora.ToString("yyyy-MM-ddTHH:mm:ss"),
                    lugarPartido = "Estadio Municipal", // Asumiendo un valor por defecto
                    p.GolesLocal,
                    p.GolesVisitante,
                    estado = p.Estatus,
                    equipoLocal = new
                    {
                        p.EquipoLocal.EquipoID,
                        nombreEquipo = p.EquipoLocal.NombreEquipo,
                        logo = p.EquipoLocal.LogoURL
                    },
                    equipoVisitante = new
                    {
                        p.EquipoVisitante.EquipoID,
                        nombreEquipo = p.EquipoVisitante.NombreEquipo,
                        logo = p.EquipoVisitante.LogoURL
                    }
                })
                .ToListAsync();

            return Ok(new { success = true, data = partidos });
        }

        // GET: api/jugador/proximos-partidos
        [HttpGet("proximos-partidos")]
        public async Task<IActionResult> ObtenerProximosPartidos()
        {
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var jugador = await _context.Jugadores.FirstOrDefaultAsync(j => j.UsuarioID == usuarioId);

            if (jugador == null)
                return NotFound(new { success = false, message = "Jugador no encontrado" });

            var partidos = await _context.Partidos
                .Include(p => p.EquipoLocal)
                .Include(p => p.EquipoVisitante)
                .Where(p => (p.EquipoLocalID == jugador.EquipoID || p.EquipoVisitanteID == jugador.EquipoID)
                    && (p.Estatus == "Programado" || p.Estatus == "EnJuego"))
                .OrderBy(p => p.FechaHora)
                .Select(p => new
                {
                    p.PartidoID,
                    p.EquipoLocalID,
                    p.EquipoVisitanteID,
                    fechaPartido = p.FechaHora.ToString("yyyy-MM-ddTHH:mm:ss"),
                    lugarPartido = "Estadio Municipal",
                    p.GolesLocal,
                    p.GolesVisitante,
                    estado = p.Estatus,
                    equipoLocal = new
                    {
                        p.EquipoLocal.EquipoID,
                        nombreEquipo = p.EquipoLocal.NombreEquipo,
                        logo = p.EquipoLocal.LogoURL
                    },
                    equipoVisitante = new
                    {
                        p.EquipoVisitante.EquipoID,
                        nombreEquipo = p.EquipoVisitante.NombreEquipo,
                        logo = p.EquipoVisitante.LogoURL
                    }
                })
                .ToListAsync();

            return Ok(new { success = true, data = partidos });
        }

        // GET: api/jugador/equipo
        [HttpGet("equipo")]
        public async Task<IActionResult> ObtenerEquipo()
        {
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var jugador = await _context.Jugadores.FirstOrDefaultAsync(j => j.UsuarioID == usuarioId);

            if (jugador == null)
                return NotFound(new { success = false, message = "Jugador no encontrado" });

            var equipo = await _context.Equipos.FindAsync(jugador.EquipoID);

            if (equipo == null)
                return NotFound(new { success = false, message = "Equipo no encontrado" });

            return Ok(new
            {
                success = true,
                data = new
                {
                    equipo.EquipoID,
                    nombreEquipo = equipo.NombreEquipo,
                    logo = equipo.LogoURL,
                    fechaCreacion = equipo.FechaRegistro,
                    equipo.CapitanID,
                    equipo.Activo
                }
            });
        }

        // GET: api/jugador/equipo/jugadores
        [HttpGet("equipo/jugadores")]
        public async Task<IActionResult> ObtenerJugadoresEquipo()
        {
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var jugador = await _context.Jugadores.FirstOrDefaultAsync(j => j.UsuarioID == usuarioId);

            if (jugador == null)
                return NotFound(new { success = false, message = "Jugador no encontrado" });

            var jugadores = await _context.Jugadores
                .Include(j => j.Usuario)
                .Where(j => j.EquipoID == jugador.EquipoID)
                .Select(j => new
                {
                    j.JugadorID,
                    j.UsuarioID,
                    numeroCamiseta = j.NumeroJugador,
                    posicion = j.Posicion,
                    esCapitan = j.Estatus == "Capitan",
                    fechaIngreso = j.FechaRegistro,
                    usuario = new
                    {
                        nombre = j.Usuario.Nombre,
                        email = j.Usuario.Email,
                        telefono = j.Usuario.Telefono
                    }
                })
                .ToListAsync();

            return Ok(new { success = true, data = jugadores });
        }

        // POST: api/jugador/equipo/generar-qr
        [HttpPost("equipo/generar-qr")]
        public async Task<IActionResult> GenerarQRJugadores()
        {
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var jugador = await _context.Jugadores.FirstOrDefaultAsync(j => j.UsuarioID == usuarioId);

            if (jugador == null)
                return NotFound(new { success = false, message = "Jugador no encontrado" });

            // Verificar si es capitán
            var equipo = await _context.Equipos.FindAsync(jugador.EquipoID);
            if (equipo.CapitanID != usuarioId)
                return BadRequest(new { success = false, message = "Solo los capitanes pueden generar QR" });

            // Generar token QR
            var invitacionID = new Random().Next(100000, 999999);
            var expiracion = DateTime.Now.AddDays(7).ToString("yyyy-MM-ddTHH:mm:ss");
            var token = $"JUGADOR|{expiracion}|{jugador.EquipoID}|{invitacionID}";

            return Ok(new { success = true, data = token });
        }

        // PUT: api/jugador/actualizar
        [HttpPut("actualizar")]
        public async Task<IActionResult> ActualizarJugador([FromBody] ActualizarJugadorRequest request)
        {
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var jugador = await _context.Jugadores.FirstOrDefaultAsync(j => j.UsuarioID == usuarioId);

            if (jugador == null)
                return NotFound(new { success = false, message = "Jugador no encontrado" });

            jugador.NumeroJugador = request.NumeroCamiseta;
            jugador.Posicion = request.Posicion;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Jugador actualizado correctamente" });
        }

        // PUT: api/jugador/actualizar-perfil-completo
        [HttpPut("actualizar-perfil-completo")]
        public async Task<IActionResult> ActualizarPerfilCompleto([FromBody] ActualizarPerfilCompletoRequest request)
        {
            try
            {
                var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                // Obtener jugador con usuario
                var jugador = await _context.Jugadores
                    .Include(j => j.Usuario)
                    .FirstOrDefaultAsync(j => j.UsuarioID == usuarioId);

                if (jugador == null)
                    return NotFound(new { success = false, message = "Jugador no encontrado" });

                var usuario = jugador.Usuario;

                // Verificar si el email ya existe en otro usuario
                if (request.Email != usuario.Email)
                {
                    var emailExiste = await _context.Usuarios.AnyAsync(u => u.Email == request.Email && u.UsuarioID != usuarioId);
                    if (emailExiste)
                        return BadRequest(new { success = false, message = "El email ya está en uso" });
                }

                // Verificar si el número de camiseta ya existe en el equipo
                if (request.NumeroCamiseta != jugador.NumeroJugador)
                {
                    var numeroExiste = await _context.Jugadores.AnyAsync(j =>
                        j.EquipoID == jugador.EquipoID &&
                        j.NumeroJugador == request.NumeroCamiseta &&
                        j.JugadorID != jugador.JugadorID);

                    if (numeroExiste)
                        return BadRequest(new { success = false, message = "Ese número de camiseta ya está en uso en tu equipo" });
                }

                // Actualizar datos del usuario
                usuario.Nombre = request.Nombre;
                usuario.Email = request.Email;
                usuario.Telefono = request.Telefono;

                // Actualizar datos del jugador
                jugador.NumeroJugador = request.NumeroCamiseta;
                jugador.Posicion = request.Posicion;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Perfil actualizado correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error al actualizar: {ex.Message}" });
            }
        }

        public class ActualizarPerfilCompletoRequest
        {
            public string Nombre { get; set; }
            public string Email { get; set; }
            public string? Telefono { get; set; }
            public int NumeroCamiseta { get; set; }
            public string Posicion { get; set; }
        }
    }

    public class ActualizarJugadorRequest
    {
        public int NumeroCamiseta { get; set; }
        public string Posicion { get; set; }
    }
}