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
    [Authorize(Roles = "Arbitro")]
    public class ArbitroController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ArbitroController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/arbitro/mi-perfil
        [HttpGet("mi-perfil")]
        public async Task<IActionResult> ObtenerMiPerfil()
        {
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var arbitro = await _context.Usuarios
                .Where(u => u.UsuarioID == usuarioId)
                .Select(u => new
                {
                    arbitroID = u.UsuarioID,
                    usuarioID = u.UsuarioID,
                    licencia = "ARB-2025-" + u.UsuarioID.ToString("D3"),
                    fechaRegistro = u.FechaRegistro,
                    activo = u.Activo,
                    usuario = new
                    {
                        u.UsuarioID,
                        u.Nombre,
                        u.Email,
                        u.Telefono
                    }
                })
                .FirstOrDefaultAsync();

            if (arbitro == null)
                return NotFound(new { success = false, message = "Árbitro no encontrado" });

            return Ok(new { success = true, data = arbitro });
        }

        // GET: api/arbitro/estadisticas
        [HttpGet("estadisticas")]
        public async Task<IActionResult> ObtenerEstadisticas()
        {
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var partidosArbitrados = await _context.Partidos
                .Where(p => p.ArbitroID == usuarioId && p.Estatus == "Finalizado")
                .CountAsync();

            var tarjetasAmarillas = await _context.EventosPartido
                .Where(e => e.TipoEvento == "TarjetaAmarilla")
                .Join(_context.Partidos.Where(p => p.ArbitroID == usuarioId),
                    e => e.PartidoID, p => p.PartidoID, (e, p) => e)
                .CountAsync();

            var tarjetasRojas = await _context.EventosPartido
                .Where(e => e.TipoEvento == "TarjetaRoja")
                .Join(_context.Partidos.Where(p => p.ArbitroID == usuarioId),
                    e => e.PartidoID, p => p.PartidoID, (e, p) => e)
                .CountAsync();

            var promedioTarjetas = partidosArbitrados > 0
                ? (double)(tarjetasAmarillas + tarjetasRojas) / partidosArbitrados
                : 0;

            return Ok(new
            {
                success = true,
                data = new
                {
                    arbitroID = usuarioId,
                    partidosArbitrados,
                    tarjetasAmarillasOtorgadas = tarjetasAmarillas,
                    tarjetasRojasOtorgadas = tarjetasRojas,
                    promedioTarjetasPorPartido = Math.Round(promedioTarjetas, 2)
                }
            });
        }

        // GET: api/arbitro/partidos
        [HttpGet("partidos")]
        public async Task<IActionResult> ObtenerPartidos()
        {
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var partidos = await _context.Partidos
                .Include(p => p.EquipoLocal)
                .Include(p => p.EquipoVisitante)
                .Where(p => p.ArbitroID == usuarioId)
                .OrderByDescending(p => p.FechaHora)
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

        // GET: api/arbitro/proximos-partidos
        [HttpGet("proximos-partidos")]
        public async Task<IActionResult> ObtenerProximosPartidos()
        {
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var partidos = await _context.Partidos
                .Include(p => p.EquipoLocal)
                .Include(p => p.EquipoVisitante)
                .Where(p => p.ArbitroID == usuarioId
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
    }
}