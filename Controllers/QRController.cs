using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TorneoManager.API.Data;
using TorneoManager.API.Models;
using System.Security.Claims;
using QRCoder;

namespace TorneoManager.API.Controllers
{
    [Route("api/qr")]
    [ApiController]
    public class QRController : ControllerBase
    {
        private readonly AppDbContext _context;

        public QRController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("generar")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GenerarQR([FromBody] GenerarQRRequest request)
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var codigoUnico = Guid.NewGuid().ToString();

            var qr = new CodigoQR
            {
                TorneoID = request.TorneoID,
                CodigoUnico = codigoUnico,
                FechaExpiracion = DateTime.Now.AddDays(request.DiasValidez),
                GeneradoPorID = adminId
            };
            _context.CodigosQR.Add(qr);
            await _context.SaveChangesAsync();

            // Generar imagen QR
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(codigoUnico, QRCodeGenerator.ECCLevel.Q))
            using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
            {
                byte[] qrCodeImage = qrCode.GetGraphic(20);
                return File(qrCodeImage, "image/png", $"QR_{qr.CodigoQRID}.png");
            }
        }

        [HttpGet("{codigo}/validar")]
        public async Task<IActionResult> ValidarQR(string codigo)
        {
            var qr = await _context.CodigosQR.FirstOrDefaultAsync(q => q.CodigoUnico == codigo);

            if (qr == null)
                return NotFound(new { valido = false, mensaje = "Código no encontrado" });

            if (qr.Usado)
                return Ok(new { valido = false, mensaje = "Código ya utilizado" });

            if (qr.FechaExpiracion < DateTime.Now)
                return Ok(new { valido = false, mensaje = "Código expirado" });

            return Ok(new { valido = true, torneoID = qr.TorneoID });
        }
    }

    public class GenerarQRRequest
    {
        public int TorneoID { get; set; }
        public int DiasValidez { get; set; } = 7;
    }
}