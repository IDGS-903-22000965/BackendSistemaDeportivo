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
    public class PartidosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PartidosController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetPartidos([FromQuery] int? torneoId)
        {
            var query = _context.Partidos.AsQueryable();
            if (torneoId.HasValue)
                query = query.Where(p => p.TorneoID == torneoId.Value);

            var partidos = await query.OrderBy(p => p.FechaHora).ToListAsync();
            return Ok(partidos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPartido(int id)
        {
            var partido = await _context.Partidos.FindAsync(id);
            if (partido == null) return NotFound();
            return Ok(partido);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CreatePartido([FromBody] Partido partido)
        {
            _context.Partidos.Add(partido);
            await _context.SaveChangesAsync();
            return Ok(partido);
        }

        [HttpGet("mis-partidos")]
        public async Task<IActionResult> GetMisPartidos()
        {
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var equipos = await _context.Equipos.Where(e => e.CapitanID == usuarioId).Select(e => e.EquipoID).ToListAsync();

            var partidos = await _context.Partidos
                .Where(p => equipos.Contains(p.EquipoLocalID) || equipos.Contains(p.EquipoVisitanteID))
                .OrderBy(p => p.FechaHora)
                .ToListAsync();

            return Ok(partidos);
        }

        [HttpGet("arbitrar")]
        [Authorize(Roles = "Arbitro")]
        public async Task<IActionResult> GetPartidosArbitrar()
        {
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var partidos = await _context.Partidos.Where(p => p.ArbitroID == usuarioId).ToListAsync();
            return Ok(partidos);
        }

        [HttpPost("{id}/iniciar")]
        [Authorize(Roles = "Arbitro")]
        public async Task<IActionResult> IniciarPartido(int id)
        {
            var partido = await _context.Partidos.FindAsync(id);
            if (partido == null) return NotFound();

            partido.Estatus = "EnJuego";
            await _context.SaveChangesAsync();
            return Ok(partido);
        }

        [HttpPost("{id}/finalizar")]
        [Authorize(Roles = "Arbitro")]
        public async Task<IActionResult> FinalizarPartido(int id)
        {
            var partido = await _context.Partidos.FindAsync(id);
            if (partido == null) return NotFound();

            partido.Estatus = "Finalizado";
            partido.FechaFinalizacion = DateTime.Now;
            await _context.SaveChangesAsync();
            return Ok(partido);
        }

        [HttpPost("{id}/eventos/gol")]
        [Authorize(Roles = "Arbitro")]
        public async Task<IActionResult> RegistrarGol(int id, [FromBody] EventoRequest request)
        {
            var evento = new EventoPartido
            {
                PartidoID = id,
                JugadorID = request.JugadorID,
                TipoEvento = "Gol",
                Minuto = request.Minuto,
                AsistenciaJugadorID = request.AsistenciaJugadorID,
                Comentarios = request.Comentarios
            };
            _context.EventosPartido.Add(evento);

            var partido = await _context.Partidos.FindAsync(id);
            var jugador = await _context.Jugadores.FindAsync(request.JugadorID);

            if (partido.EquipoLocalID == jugador.EquipoID)
                partido.GolesLocal++;
            else
                partido.GolesVisitante++;

            await _context.SaveChangesAsync();
            return Ok(evento);
        }

        [HttpPost("{id}/eventos/tarjeta")]
        [Authorize(Roles = "Arbitro")]
        public async Task<IActionResult> RegistrarTarjeta(int id, [FromBody] TarjetaRequest request)
        {
            var evento = new EventoPartido
            {
                PartidoID = id,
                JugadorID = request.JugadorID,
                TipoEvento = request.Tipo == "Amarilla" ? "TarjetaAmarilla" : "TarjetaRoja",
                Minuto = request.Minuto,
                Comentarios = request.Motivo
            };
            _context.EventosPartido.Add(evento);
            await _context.SaveChangesAsync();

            // Verificar sanciones
            var partido = await _context.Partidos.FindAsync(id);
            var reglas = await _context.ReglasTorneo.FirstAsync(r => r.TorneoID == partido.TorneoID);

            if (request.Tipo == "Roja")
            {
                var sancion = new Sancion
                {
                    JugadorID = request.JugadorID,
                    TorneoID = partido.TorneoID,
                    TipoSancion = "TarjetaRoja",
                    PartidosSuspension = reglas.PartidosSuspensionTarjetaRoja,
                    FechaInicio = DateTime.Now,
                    Motivo = request.Motivo,
                    EventoRelacionadoID = evento.EventoID
                };
                _context.Sanciones.Add(sancion);

                var jugador = await _context.Jugadores.FindAsync(request.JugadorID);
                jugador.Estatus = "Suspendido";
            }
            else
            {
                var amarillas = await _context.EventosPartido
                    .Where(e => e.JugadorID == request.JugadorID && e.TipoEvento == "TarjetaAmarilla")
                    .Join(_context.Partidos.Where(p => p.TorneoID == partido.TorneoID), e => e.PartidoID, p => p.PartidoID, (e, p) => e)
                    .CountAsync();

                if (amarillas >= reglas.TarjetasAmarillasParaSuspension)
                {
                    var sancion = new Sancion
                    {
                        JugadorID = request.JugadorID,
                        TorneoID = partido.TorneoID,
                        TipoSancion = "AcumulacionAmarillas",
                        PartidosSuspension = 1,
                        FechaInicio = DateTime.Now,
                        Motivo = $"Acumulación de {amarillas} tarjetas amarillas",
                        EventoRelacionadoID = evento.EventoID
                    };
                    _context.Sanciones.Add(sancion);

                    var jugador = await _context.Jugadores.FindAsync(request.JugadorID);
                    jugador.Estatus = "Suspendido";
                }
            }

            await _context.SaveChangesAsync();
            return Ok(evento);
        }

        [HttpPost("{id}/incidentes")]
        [Authorize(Roles = "Arbitro")]
        public async Task<IActionResult> ReportarIncidente(int id, [FromBody] IncidenteRequest request)
        {
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var incidente = new IncidentePartido
            {
                PartidoID = id,
                ArbitroID = usuarioId,
                TipoIncidente = request.TipoIncidente,
                Descripcion = request.Descripcion,
                Gravedad = request.Gravedad,
                EvidenciaURL = request.EvidenciaURL
            };
            _context.IncidentesPartido.Add(incidente);
            await _context.SaveChangesAsync();
            return Ok(incidente);
        }

        [HttpGet("{id}/eventos")]
        public async Task<IActionResult> GetEventos(int id)
        {
            var eventos = await _context.EventosPartido.Where(e => e.PartidoID == id).OrderBy(e => e.Minuto).ToListAsync();
            return Ok(eventos);
        }
    }

    public class EventoRequest
    {
        public int JugadorID { get; set; }
        public int? AsistenciaJugadorID { get; set; }
        public int Minuto { get; set; }
        public string? Comentarios { get; set; }
    }

    public class TarjetaRequest
    {
        public int JugadorID { get; set; }
        public string Tipo { get; set; }
        public int Minuto { get; set; }
        public string Motivo { get; set; }
    }

    public class IncidenteRequest
    {
        public string TipoIncidente { get; set; }
        public string Descripcion { get; set; }
        public string Gravedad { get; set; }
        public string? EvidenciaURL { get; set; }
    }
}