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
    public class PartidoController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PartidoController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/partido/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPartido(int id)
        {
            var partido = await _context.Partidos
                .Include(p => p.EquipoLocal)
                .Include(p => p.EquipoVisitante)
                .Where(p => p.PartidoID == id)
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
                    fechaFinalizacion = p.FechaFinalizacion,
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
                    },
                    arbitro = p.ArbitroID != null ? new
                    {
                        arbitroID = p.ArbitroID,
                        nombre = _context.Usuarios.Where(u => u.UsuarioID == p.ArbitroID).Select(u => u.Nombre).FirstOrDefault()
                    } : null
                })
                .FirstOrDefaultAsync();

            if (partido == null)
                return NotFound(new { success = false, message = "Partido no encontrado" });

            return Ok(new { success = true, data = partido });
        }

        // GET: api/partido/{id}/eventos
        [HttpGet("{id}/eventos")]
        public async Task<IActionResult> ObtenerEventosPartido(int id)
        {
            var eventos = await _context.EventosPartido
                .Include(e => e.Jugador)
                .ThenInclude(j => j.Usuario)
                .Where(e => e.PartidoID == id)
                .OrderBy(e => e.Minuto)
                .Select(e => new
                {
                    e.EventoID,
                    e.PartidoID,
                    e.JugadorID,
                    tipoEvento = e.TipoEvento,
                    minuto = e.Minuto,
                    asistenciaJugadorID = e.AsistenciaJugadorID,
                    descripcion = e.Comentarios,
                    fechaHoraEvento = e.FechaHoraEvento,
                    jugador = new
                    {
                        e.Jugador.JugadorID,
                        numeroCamiseta = e.Jugador.NumeroJugador,
                        posicion = e.Jugador.Posicion,
                        usuario = new
                        {
                            nombre = e.Jugador.Usuario.Nombre
                        }
                    }
                })
                .ToListAsync();

            return Ok(new { success = true, data = eventos });
        }

        // GET: api/partido/{id}/jugadores
        [HttpGet("{id}/jugadores")]
        public async Task<IActionResult> ObtenerJugadoresPartido(int id)
        {
            var partido = await _context.Partidos.FindAsync(id);
            if (partido == null)
                return NotFound(new { success = false, message = "Partido no encontrado" });

            var jugadoresLocal = await _context.Jugadores
                .Include(j => j.Usuario)
                .Where(j => j.EquipoID == partido.EquipoLocalID)
                .Select(j => new
                {
                    j.JugadorID,
                    numeroCamiseta = j.NumeroJugador,
                    posicion = j.Posicion,
                    nombre = j.Usuario.Nombre
                })
                .ToListAsync();

            var jugadoresVisitante = await _context.Jugadores
                .Include(j => j.Usuario)
                .Where(j => j.EquipoID == partido.EquipoVisitanteID)
                .Select(j => new
                {
                    j.JugadorID,
                    numeroCamiseta = j.NumeroJugador,
                    posicion = j.Posicion,
                    nombre = j.Usuario.Nombre
                })
                .ToListAsync();

            var equipoLocal = await _context.Equipos.FindAsync(partido.EquipoLocalID);
            var equipoVisitante = await _context.Equipos.FindAsync(partido.EquipoVisitanteID);

            return Ok(new
            {
                success = true,
                data = new
                {
                    equipoLocal = new
                    {
                        equipoLocal.EquipoID,
                        nombreEquipo = equipoLocal.NombreEquipo,
                        jugadores = jugadoresLocal
                    },
                    equipoVisitante = new
                    {
                        equipoVisitante.EquipoID,
                        nombreEquipo = equipoVisitante.NombreEquipo,
                        jugadores = jugadoresVisitante
                    }
                }
            });
        }

        // POST: api/partido/{id}/iniciar
        [HttpPost("{id}/iniciar")]
        [Authorize(Roles = "Arbitro")]
        public async Task<IActionResult> IniciarPartido(int id)
        {
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var partido = await _context.Partidos.FindAsync(id);

            if (partido == null)
                return NotFound(new { success = false, message = "Partido no encontrado" });

            if (partido.ArbitroID != usuarioId)
                return BadRequest(new { success = false, message = "No eres el árbitro de este partido" });

            if (partido.Estatus != "Programado")
                return BadRequest(new { success = false, message = "El partido no está en estado Programado" });

            partido.Estatus = "En Curso";
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Partido iniciado correctamente" });
        }

        // POST: api/partido/{id}/finalizar
        [HttpPost("{id}/finalizar")]
        [Authorize(Roles = "Arbitro")]
        public async Task<IActionResult> FinalizarPartido(int id)
        {
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var partido = await _context.Partidos.FindAsync(id);

            if (partido == null)
                return NotFound(new { success = false, message = "Partido no encontrado" });

            if (partido.ArbitroID != usuarioId)
                return BadRequest(new { success = false, message = "No eres el árbitro de este partido" });

            if (partido.Estatus != "EnJuego" && partido.Estatus != "En Curso")
                return BadRequest(new { success = false, message = "El partido no está en curso" });

            partido.Estatus = "Finalizado";
            partido.FechaFinalizacion = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Partido finalizado correctamente" });
        }

        // POST: api/partido/evento
        [HttpPost("evento")]
        [Authorize(Roles = "Arbitro")]
        public async Task<IActionResult> RegistrarEvento([FromBody] RegistrarEventoRequest request)
        {
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var partido = await _context.Partidos.FindAsync(request.PartidoID);

            if (partido == null)
                return NotFound(new { success = false, message = "Partido no encontrado" });

            if (partido.ArbitroID != usuarioId)
                return BadRequest(new { success = false, message = "No eres el árbitro de este partido" });

            if (partido.Estatus != "EnJuego" && partido.Estatus != "En Curso")
                return BadRequest(new { success = false, message = "El partido debe estar en curso" });

            var evento = new EventoPartido
            {
                PartidoID = request.PartidoID,
                JugadorID = request.JugadorID,
                TipoEvento = request.TipoEvento,
                Minuto = request.Minuto,
                AsistenciaJugadorID = request.AsistenciaJugadorID,
                Comentarios = request.Descripcion,
                FechaHoraEvento = DateTime.Now
            };

            _context.EventosPartido.Add(evento);
            await _context.SaveChangesAsync();

            // Si es un gol, actualizar marcador
            if (request.TipoEvento == "Gol")
            {
                var jugador = await _context.Jugadores.FindAsync(request.JugadorID);
                if (jugador.EquipoID == partido.EquipoLocalID)
                    partido.GolesLocal++;
                else
                    partido.GolesVisitante++;

                await _context.SaveChangesAsync();
            }

            // Cargar información del jugador para la respuesta
            var eventoConJugador = await _context.EventosPartido
                .Include(e => e.Jugador)
                .ThenInclude(j => j.Usuario)
                .Where(e => e.EventoID == evento.EventoID)
                .Select(e => new
                {
                    e.EventoID,
                    e.PartidoID,
                    e.JugadorID,
                    tipoEvento = e.TipoEvento,
                    minuto = e.Minuto,
                    descripcion = e.Comentarios,
                    jugador = new
                    {
                        e.Jugador.JugadorID,
                        numeroCamiseta = e.Jugador.NumeroJugador,
                        usuario = new
                        {
                            nombre = e.Jugador.Usuario.Nombre
                        }
                    }
                })
                .FirstOrDefaultAsync();

            return Ok(new
            {
                success = true,
                message = "Evento registrado correctamente",
                data = eventoConJugador
            });
        }

        // DELETE: api/partido/evento/{id}
        [HttpDelete("evento/{id}")]
        [Authorize(Roles = "Arbitro")]
        public async Task<IActionResult> EliminarEvento(int id)
        {
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var evento = await _context.EventosPartido.FindAsync(id);

            if (evento == null)
                return NotFound(new { success = false, message = "Evento no encontrado" });

            var partido = await _context.Partidos.FindAsync(evento.PartidoID);

            if (partido.ArbitroID != usuarioId)
                return BadRequest(new { success = false, message = "No eres el árbitro de este partido" });

            if (partido.Estatus == "Finalizado")
                return BadRequest(new { success = false, message = "No se pueden eliminar eventos de partidos finalizados" });

            // Si era un gol, restar del marcador
            if (evento.TipoEvento == "Gol")
            {
                var jugador = await _context.Jugadores.FindAsync(evento.JugadorID);
                if (jugador.EquipoID == partido.EquipoLocalID)
                    partido.GolesLocal--;
                else
                    partido.GolesVisitante--;
            }

            _context.EventosPartido.Remove(evento);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Evento eliminado correctamente" });
        }
    }

    public class RegistrarEventoRequest
    {
        public int PartidoID { get; set; }
        public int JugadorID { get; set; }
        public string TipoEvento { get; set; } // "Gol", "Asistencia", "TarjetaAmarilla", "TarjetaRoja"
        public int Minuto { get; set; }
        public int? AsistenciaJugadorID { get; set; }
        public string? Descripcion { get; set; }
    }
}