using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TorneoManager.API.Data;
using TorneoManager.API.Models;

namespace TorneoManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CanchasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CanchasController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetCanchas()
        {
            var canchas = await _context.Canchas.ToListAsync();
            return Ok(canchas);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCancha(int id)
        {
            var cancha = await _context.Canchas.FindAsync(id);
            if (cancha == null) return NotFound();
            return Ok(cancha);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CreateCancha([FromBody] Cancha cancha)
        {
            _context.Canchas.Add(cancha);
            await _context.SaveChangesAsync();
            return Ok(cancha);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> UpdateCancha(int id, [FromBody] Cancha cancha)
        {
            var canchaExistente = await _context.Canchas.FindAsync(id);
            if (canchaExistente == null) return NotFound();

            canchaExistente.NombreCancha = cancha.NombreCancha;
            canchaExistente.Direccion = cancha.Direccion;
            canchaExistente.Latitud = cancha.Latitud;
            canchaExistente.Longitud = cancha.Longitud;
            canchaExistente.Capacidad = cancha.Capacidad;
            canchaExistente.TipoSuperficie = cancha.TipoSuperficie;
            canchaExistente.Activo = cancha.Activo;

            await _context.SaveChangesAsync();
            return Ok(canchaExistente);
        }
    }
}