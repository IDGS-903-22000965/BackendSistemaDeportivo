using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TorneoManager.API.Data;
using TorneoManager.API.Models;

namespace TorneoManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (usuario == null || !BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
                return Unauthorized(new { mensaje = "Credenciales inválidas" });

            var roles = await _context.UsuarioRoles
                .Where(ur => ur.UsuarioID == usuario.UsuarioID)
                .Join(_context.Roles, ur => ur.RolID, r => r.RolID, (ur, r) => r.NombreRol)
                .ToListAsync();

            var token = GenerarToken(usuario, roles);

            return Ok(new { token, usuario = new { usuario.UsuarioID, usuario.Email, usuario.Nombre, roles } });
        }

        [HttpPost("register-with-qr")]
        public async Task<IActionResult> RegisterWithQR([FromBody] RegisterRequest request)
        {
            var qr = await _context.CodigosQR.FirstOrDefaultAsync(q => q.CodigoUnico == request.CodigoQR && !q.Usado);

            if (qr == null || qr.FechaExpiracion < DateTime.Now)
                return BadRequest(new { mensaje = "Código QR inválido o expirado" });

            if (await _context.Usuarios.AnyAsync(u => u.Email == request.Email))
                return BadRequest(new { mensaje = "Email ya registrado" });

            var usuario = new Usuario
            {
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Nombre = request.Nombre,
                Apellidos = request.Apellidos,
                Telefono = request.Telefono
            };
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            var rolCapitan = await _context.Roles.FirstAsync(r => r.NombreRol == "Capitan");
            _context.UsuarioRoles.Add(new UsuarioRol { UsuarioID = usuario.UsuarioID, RolID = rolCapitan.RolID });

            var equipo = new Equipo
            {
                NombreEquipo = request.NombreEquipo,
                ColorUniformePrimario = request.ColorPrimario,
                ColorUniformeSecundario = request.ColorSecundario,
                CapitanID = usuario.UsuarioID
            };
            _context.Equipos.Add(equipo);
            await _context.SaveChangesAsync();

            qr.Usado = true;
            qr.EquipoRegistradoID = equipo.EquipoID;
            qr.FechaUso = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Registro exitoso", equipoID = equipo.EquipoID });
        }

        private string GenerarToken(Usuario usuario, List<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.UsuarioID.ToString()),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Name, $"{usuario.Nombre} {usuario.Apellidos}")
            };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                _config["JwtSettings:Issuer"],
                _config["JwtSettings:Audience"],
                claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(_config["JwtSettings:ExpirationMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        [HttpPost("crear-admin-temporal")]
        [AllowAnonymous]
        public async Task<IActionResult> CrearAdminTemporal()
        {
            // Verificar si ya existe
            if (await _context.Usuarios.AnyAsync(u => u.Email == "admin@torneo.com"))
                return BadRequest(new { mensaje = "Admin ya existe" });

            // Crear usuario
            var usuario = new Usuario
            {
                Email = "admin@torneo.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Nombre = "Admin",
                Apellidos = "Principal",
                Activo = true
            };
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            // Asignar rol de Administrador
            var rolAdmin = await _context.Roles.FirstOrDefaultAsync(r => r.NombreRol == "Administrador");
            if (rolAdmin == null)
            {
                // Crear rol si no existe
                rolAdmin = new Rol { NombreRol = "Administrador", Descripcion = "Dueño de las canchas" };
                _context.Roles.Add(rolAdmin);
                await _context.SaveChangesAsync();
            }

            _context.UsuarioRoles.Add(new UsuarioRol
            {
                UsuarioID = usuario.UsuarioID,
                RolID = rolAdmin.RolID
            });
            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Admin creado exitosamente",
                email = "admin@torneo.com",
                password = "admin123",
                usuarioID = usuario.UsuarioID
            });
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class RegisterRequest
    {
        public string CodigoQR { get; set; }
        public string NombreEquipo { get; set; }
        public string ColorPrimario { get; set; }
        public string ColorSecundario { get; set; }
        public string Nombre { get; set; }
        public string Apellidos { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Telefono { get; set; }
    }
}