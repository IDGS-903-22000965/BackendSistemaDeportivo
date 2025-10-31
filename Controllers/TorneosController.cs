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
    public class TorneosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TorneosController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetTorneos()
        {
            var torneos = await _context.Torneos.Where(t => t.Activo).ToListAsync();
            return Ok(torneos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTorneo(int id)
        {
            var torneo = await _context.Torneos.FindAsync(id);
            if (torneo == null) return NotFound();
            return Ok(torneo);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CreateTorneo([FromBody] Torneo torneo)
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            torneo.AdminID = adminId;

            _context.Torneos.Add(torneo);
            await _context.SaveChangesAsync();

            // Crear reglas por defecto
            var reglas = new ReglaTorneo { TorneoID = torneo.TorneoID };
            _context.ReglasTorneo.Add(reglas);
            await _context.SaveChangesAsync();

            return Ok(torneo);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> UpdateTorneo(int id, [FromBody] Torneo torneo)
        {
            var torneoExistente = await _context.Torneos.FindAsync(id);
            if (torneoExistente == null) return NotFound();

            torneoExistente.NombreTorneo = torneo.NombreTorneo;
            torneoExistente.Categoria = torneo.Categoria;
            torneoExistente.CuotaInscripcion = torneo.CuotaInscripcion;
            torneoExistente.CostoArbitraje = torneo.CostoArbitraje;

            await _context.SaveChangesAsync();
            return Ok(torneoExistente);
        }

        [HttpGet("{id}/equipos")]
        public async Task<IActionResult> GetEquiposInscritos(int id)
        {
            var equipos = await _context.InscripcionesTorneo
                .Where(i => i.TorneoID == id)
                .Join(_context.Equipos, i => i.EquipoID, e => e.EquipoID, (i, e) => e)
                .ToListAsync();
            return Ok(equipos);
        }

        [HttpPost("{id}/inscribir")]
        [Authorize(Roles = "Capitan")]
        public async Task<IActionResult> InscribirEquipo(int id, [FromBody] InscribirRequest request)
        {
            var inscripcion = new InscripcionTorneo
            {
                TorneoID = id,
                EquipoID = request.EquipoID
            };
            _context.InscripcionesTorneo.Add(inscripcion);
            await _context.SaveChangesAsync();
            return Ok(inscripcion);
        }

        [HttpGet("{id}/tabla-posiciones")]
        public async Task<IActionResult> GetTablaPosiciones(int id)
        {
            var equiposInscritos = await _context.InscripcionesTorneo
                .Where(i => i.TorneoID == id)
                .Select(i => i.EquipoID)
                .ToListAsync();

            var tabla = new List<object>();

            foreach (var equipoId in equiposInscritos)
            {
                var partidos = await _context.Partidos
                    .Where(p => p.TorneoID == id && p.Estatus == "Finalizado" &&
                           (p.EquipoLocalID == equipoId || p.EquipoVisitanteID == equipoId))
                    .ToListAsync();

                int puntos = 0, victorias = 0, empates = 0, derrotas = 0;
                int golesFavor = 0, golesContra = 0;

                foreach (var p in partidos)
                {
                    bool esLocal = p.EquipoLocalID == equipoId;
                    int gf = esLocal ? p.GolesLocal : p.GolesVisitante;
                    int gc = esLocal ? p.GolesVisitante : p.GolesLocal;

                    golesFavor += gf;
                    golesContra += gc;

                    if (gf > gc) { victorias++; puntos += 3; }
                    else if (gf == gc) { empates++; puntos += 1; }
                    else derrotas++;
                }

                var equipo = await _context.Equipos.FindAsync(equipoId);
                tabla.Add(new
                {
                    equipo.EquipoID,
                    equipo.NombreEquipo,
                    PJ = partidos.Count,
                    victorias,
                    empates,
                    derrotas,
                    golesFavor,
                    golesContra,
                    diferencia = golesFavor - golesContra,
                    puntos
                });
            }

            return Ok(tabla.OrderByDescending(t => ((dynamic)t).puntos));
        }

        [HttpGet("{id}/goleadores")]
        public async Task<IActionResult> GetGoleadores(int id)
        {
            var goleadores = await _context.EventosPartido
                .Where(e => e.TipoEvento == "Gol")
                .Join(_context.Partidos.Where(p => p.TorneoID == id), e => e.PartidoID, p => p.PartidoID, (e, p) => e)
                .GroupBy(e => e.JugadorID)
                .Select(g => new { JugadorID = g.Key, Goles = g.Count() })
                .OrderByDescending(g => g.Goles)
                .ToListAsync();

            var resultado = new List<object>();
            foreach (var g in goleadores)
            {
                var jugador = await _context.Jugadores.FindAsync(g.JugadorID);
                var usuario = await _context.Usuarios.FindAsync(jugador.UsuarioID);
                var equipo = await _context.Equipos.FindAsync(jugador.EquipoID);

                resultado.Add(new
                {
                    jugador.JugadorID,
                    Nombre = $"{usuario.Nombre} {usuario.Apellidos}",
                    equipo.NombreEquipo,
                    g.Goles
                });
            }

            return Ok(resultado);
        }
    }

    public class InscribirRequest
    {
        public int EquipoID { get; set; }
    }
}