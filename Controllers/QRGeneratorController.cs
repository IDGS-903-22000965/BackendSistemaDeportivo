using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace TorneoManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QRGeneratorController : ControllerBase
    {
        // GET: api/qrgenerator/capitan
        [HttpGet("capitan")]
        [AllowAnonymous]
        public IActionResult GenerarQRCapitan()
        {
            try
            {
                // Generar token único
                var invitacionID = new Random().Next(100000, 999999);
                var expiracion = DateTime.Now.AddDays(7).ToString("yyyy-MM-ddTHH:mm:ss");
                var token = $"CAPITAN|{expiracion}|0|{invitacionID}";

                // Generar imagen QR
                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(token, QRCodeGenerator.ECCLevel.Q))
                using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                {
                    byte[] qrCodeImage = qrCode.GetGraphic(20);
                    return File(qrCodeImage, "image/png", $"QR_Capitan_{invitacionID}.png");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Error al generar QR: {ex.Message}" });
            }
        }

        // GET: api/qrgenerator/jugador/{equipoID}
        [HttpGet("jugador/{equipoID}")]
        [AllowAnonymous]
        public IActionResult GenerarQRJugador(int equipoID)
        {
            try
            {
                // Generar token único
                var invitacionID = new Random().Next(100000, 999999);
                var expiracion = DateTime.Now.AddDays(7).ToString("yyyy-MM-ddTHH:mm:ss");
                var token = $"JUGADOR|{expiracion}|{equipoID}|{invitacionID}";

                // Generar imagen QR
                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(token, QRCodeGenerator.ECCLevel.Q))
                using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                {
                    byte[] qrCodeImage = qrCode.GetGraphic(20);
                    return File(qrCodeImage, "image/png", $"QR_Jugador_Equipo{equipoID}_{invitacionID}.png");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Error al generar QR: {ex.Message}" });
            }
        }

        // GET: api/qrgenerator/arbitro
        [HttpGet("arbitro")]
        [AllowAnonymous]
        public IActionResult GenerarQRArbitro()
        {
            try
            {
                // Generar token único
                var invitacionID = new Random().Next(100000, 999999);
                var expiracion = DateTime.Now.AddDays(7).ToString("yyyy-MM-ddTHH:mm:ss");
                var token = $"ARBITRO|{expiracion}|0|{invitacionID}";

                // Generar imagen QR
                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(token, QRCodeGenerator.ECCLevel.Q))
                using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                {
                    byte[] qrCodeImage = qrCode.GetGraphic(20);
                    return File(qrCodeImage, "image/png", $"QR_Arbitro_{invitacionID}.png");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Error al generar QR: {ex.Message}" });
            }
        }

        // GET: api/qrgenerator/capitan/info - Para ver el token sin descargar
        [HttpGet("capitan/info")]
        [AllowAnonymous]
        public IActionResult GenerarTokenCapitan()
        {
            var invitacionID = new Random().Next(100000, 999999);
            var expiracion = DateTime.Now.AddDays(7);
            var token = $"CAPITAN|{expiracion:yyyy-MM-ddTHH:mm:ss}|0|{invitacionID}";

            return Ok(new
            {
                success = true,
                data = new
                {
                    token = token,
                    tipo = "CAPITAN",
                    invitacionID = invitacionID,
                    expiracion = expiracion,
                    expiraEn = "7 días",
                    urlDescarga = $"{Request.Scheme}://{Request.Host}/api/qrgenerator/capitan",
                    instrucciones = "Abre la URL de descarga en el navegador para obtener la imagen QR"
                }
            });
        }

        // GET: api/qrgenerator/jugador/{equipoID}/info
        [HttpGet("jugador/{equipoID}/info")]
        [AllowAnonymous]
        public IActionResult GenerarTokenJugador(int equipoID)
        {
            var invitacionID = new Random().Next(100000, 999999);
            var expiracion = DateTime.Now.AddDays(7);
            var token = $"JUGADOR|{expiracion:yyyy-MM-ddTHH:mm:ss}|{equipoID}|{invitacionID}";

            return Ok(new
            {
                success = true,
                data = new
                {
                    token = token,
                    tipo = "JUGADOR",
                    equipoID = equipoID,
                    invitacionID = invitacionID,
                    expiracion = expiracion,
                    expiraEn = "7 días",
                    urlDescarga = $"{Request.Scheme}://{Request.Host}/api/qrgenerator/jugador/{equipoID}",
                    instrucciones = "Abre la URL de descarga en el navegador para obtener la imagen QR"
                }
            });
        }

        // GET: api/qrgenerator/arbitro/info
        [HttpGet("arbitro/info")]
        [AllowAnonymous]
        public IActionResult GenerarTokenArbitro()
        {
            var invitacionID = new Random().Next(100000, 999999);
            var expiracion = DateTime.Now.AddDays(7);
            var token = $"ARBITRO|{expiracion:yyyy-MM-ddTHH:mm:ss}|0|{invitacionID}";

            return Ok(new
            {
                success = true,
                data = new
                {
                    token = token,
                    tipo = "ARBITRO",
                    invitacionID = invitacionID,
                    expiracion = expiracion,
                    expiraEn = "7 días",
                    urlDescarga = $"{Request.Scheme}://{Request.Host}/api/qrgenerator/arbitro",
                    instrucciones = "Abre la URL de descarga en el navegador para obtener la imagen QR"
                }
            });
        }
    }
}