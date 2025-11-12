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
                .Join(_context.Roles, ur => ur.RolID, r => r.RolID, (ur, r) => r)
                .ToListAsync();

            var token = GenerarToken(usuario, roles.Select(r => r.NombreRol).ToList());

            var primerRol = roles.FirstOrDefault();

            return Ok(new
            {
                token,
                usuario = new
                {
                    usuario.UsuarioID,
                    usuario.Email,
                    usuario.Nombre,
                    usuario.Telefono,
                    usuario.FechaRegistro,
                    usuario.Activo,
                    rolID = primerRol?.RolID ?? 0
                },
                rol = primerRol != null ? new
                {
                    primerRol.RolID,
                    primerRol.NombreRol,
                    primerRol.Descripcion
                } : null
            });
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

        // Apis de Sergio
        // GET: api/auth/validar-qr/{token}
        [HttpGet("validar-qr/{token}")]
        [AllowAnonymous]
        public IActionResult ValidarQR(string token)
        {
            try
            {
                // Formato: TIPO|FECHA_EXPIRACION|EQUIPO_ID|INVITACION_ID
                var partes = token.Split('|');

                if (partes.Length < 4)
                    return BadRequest(new { success = false, message = "Código QR inválido" });

                var tipo = partes[0];
                var fechaExpiracion = DateTime.Parse(partes[1]);
                var equipoID = tipo != "CAPITAN" && tipo != "ARBITRO" ? int.Parse(partes[2]) : (int?)null;
                var invitacionID = int.Parse(partes[3]);

                if (fechaExpiracion < DateTime.Now)
                    return BadRequest(new { success = false, message = "Código QR expirado" });

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        type = tipo,
                        token = token,
                        equipoID = equipoID,
                        invitacionID = invitacionID,
                        expiration = fechaExpiracion.ToString("yyyy-MM-ddTHH:mm:ss")
                    }
                });
            }
            catch
            {
                return BadRequest(new { success = false, message = "Código QR inválido" });
            }
        }

        // POST: api/auth/registro-capitan
        [HttpPost("registro-capitan")]
        [AllowAnonymous]
        public async Task<IActionResult> RegistroCapitan([FromBody] RegistroCapitanRequest request)
        {
            try
            {
                // Validar token
                var partes = request.Token.Split('|');
                if (partes[0] != "CAPITAN")
                    return BadRequest(new { success = false, message = "Token inválido para registro de capitán" });

                var fechaExpiracion = DateTime.Parse(partes[1]);
                if (fechaExpiracion < DateTime.Now)
                    return BadRequest(new { success = false, message = "Token expirado" });

                // Verificar email
                if (await _context.Usuarios.AnyAsync(u => u.Email == request.Email))
                    return BadRequest(new { success = false, message = "El email ya está registrado" });

                // Crear usuario
                var usuario = new Usuario
                {
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Nombre = request.Nombre,
                    Apellidos = "",
                    Telefono = request.Telefono,
                    Activo = true
                };
                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                // Asignar rol Jugador
                var rolJugador = await _context.Roles.FirstAsync(r => r.NombreRol == "Jugador");
                _context.UsuarioRoles.Add(new UsuarioRol
                {
                    UsuarioID = usuario.UsuarioID,
                    RolID = rolJugador.RolID
                });
                await _context.SaveChangesAsync();

                // Crear equipo
                var equipo = new Equipo
                {
                    NombreEquipo = request.NombreEquipo,
                    LogoURL = request.Logo,
                    CapitanID = usuario.UsuarioID,
                    Activo = true
                };
                _context.Equipos.Add(equipo);
                await _context.SaveChangesAsync();

                // Crear jugador (capitán)
                var jugador = new Jugador
                {
                    UsuarioID = usuario.UsuarioID,
                    EquipoID = equipo.EquipoID,
                    NumeroJugador = 1,
                    Posicion = "Mediocampista",
                    Estatus = "Capitan",
                    FechaRegistro = DateTime.Now
                };
                _context.Jugadores.Add(jugador);
                await _context.SaveChangesAsync();

                // Generar token JWT
                var roles = new List<string> { "Jugador" };
                var token = GenerarToken(usuario, roles);

                return Ok(new
                {
                    success = true,
                    message = "Registro exitoso",
                    data = new
                    {
                        token,
                        usuario = new
                        {
                            usuario.UsuarioID,
                            usuario.Email,
                            usuario.Nombre,
                            usuario.Telefono,
                            usuario.FechaRegistro,
                            usuario.Activo,
                            rolID = rolJugador.RolID
                        },
                        rol = new
                        {
                            rolJugador.RolID,
                            rolJugador.NombreRol,
                            rolJugador.Descripcion
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Error en el registro: {ex.Message}" });
            }
        }

        // POST: api/auth/registro-jugador
        [HttpPost("registro-jugador")]
        [AllowAnonymous]
        public async Task<IActionResult> RegistroJugador([FromBody] RegistroJugadorRequest request)
        {
            try
            {
                Console.WriteLine("=== INICIO REGISTRO JUGADOR ===");
                Console.WriteLine($"Token: {request.Token}");
                Console.WriteLine($"EquipoID: {request.EquipoID}");
                Console.WriteLine($"Email: {request.Email}");
                Console.WriteLine($"Nombre: {request.Nombre}");

                // Validar token
                var partes = request.Token.Split('|');
                Console.WriteLine($"Token dividido en {partes.Length} partes");

                if (partes[0] != "JUGADOR")
                {
                    Console.WriteLine($"ERROR: Token inválido. Tipo: {partes[0]}");
                    return BadRequest(new { success = false, message = "Token inválido para registro de jugador" });
                }

                var fechaExpiracion = DateTime.Parse(partes[1]);
                Console.WriteLine($"Fecha expiración: {fechaExpiracion}");

                if (fechaExpiracion < DateTime.Now)
                {
                    Console.WriteLine("ERROR: Token expirado");
                    return BadRequest(new { success = false, message = "Token expirado" });
                }

                // Verificar email
                Console.WriteLine("Verificando si el email existe...");
                var emailExiste = await _context.Usuarios.AnyAsync(u => u.Email == request.Email);
                Console.WriteLine($"Email existe: {emailExiste}");

                if (emailExiste)
                {
                    Console.WriteLine("ERROR: Email ya registrado");
                    return BadRequest(new { success = false, message = "El email ya está registrado" });
                }

                // Verificar que el equipo existe
                Console.WriteLine($"Verificando que el equipo {request.EquipoID} existe...");
                var equipoExiste = await _context.Equipos.AnyAsync(e => e.EquipoID == request.EquipoID);
                Console.WriteLine($"Equipo existe: {equipoExiste}");

                if (!equipoExiste)
                {
                    Console.WriteLine("ERROR: Equipo no encontrado");
                    return BadRequest(new { success = false, message = "Equipo no encontrado" });
                }

                // Crear usuario
                Console.WriteLine("Creando usuario...");
                var usuario = new Usuario
                {
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Nombre = request.Nombre,
                    Apellidos = "",
                    Telefono = request.Telefono,
                    Activo = true
                };
                _context.Usuarios.Add(usuario);

                Console.WriteLine("Guardando usuario en BD...");
                await _context.SaveChangesAsync();
                Console.WriteLine($"Usuario creado con ID: {usuario.UsuarioID}");

                // Asignar rol Jugador
                Console.WriteLine("Buscando rol Jugador...");
                var rolJugador = await _context.Roles.FirstOrDefaultAsync(r => r.NombreRol == "Jugador");

                if (rolJugador == null)
                {
                    Console.WriteLine("ERROR: Rol Jugador no encontrado en la BD");
                    return StatusCode(500, new { success = false, message = "Error de configuración: Rol no encontrado" });
                }

                Console.WriteLine($"Rol Jugador encontrado con ID: {rolJugador.RolID}");
                Console.WriteLine("Asignando rol al usuario...");

                _context.UsuarioRoles.Add(new UsuarioRol
                {
                    UsuarioID = usuario.UsuarioID,
                    RolID = rolJugador.RolID
                });

                await _context.SaveChangesAsync();
                Console.WriteLine("Rol asignado correctamente");

                // Crear jugador
                Console.WriteLine("Creando jugador...");
                var jugador = new Jugador
                {
                    UsuarioID = usuario.UsuarioID,
                    EquipoID = request.EquipoID,
                    NumeroJugador = request.NumeroCamiseta,
                    Posicion = request.Posicion,
                    Estatus = "Activo",
                    FechaRegistro = DateTime.Now
                };
                _context.Jugadores.Add(jugador);

                Console.WriteLine("Guardando jugador en BD...");
                await _context.SaveChangesAsync();
                Console.WriteLine($"Jugador creado con ID: {jugador.JugadorID}");

                // Generar token JWT
                Console.WriteLine("Generando token JWT...");
                var roles = new List<string> { "Jugador" };
                var token = GenerarToken(usuario, roles);
                Console.WriteLine("Token JWT generado correctamente");

                Console.WriteLine("=== REGISTRO EXITOSO ===");

                return Ok(new
                {
                    success = true,
                    message = "Registro exitoso",
                    data = new
                    {
                        token,
                        usuario = new
                        {
                            usuario.UsuarioID,
                            usuario.Email,
                            usuario.Nombre,
                            usuario.Telefono,
                            usuario.FechaRegistro,
                            usuario.Activo,
                            rolID = rolJugador.RolID
                        },
                        rol = new
                        {
                            rolJugador.RolID,
                            rolJugador.NombreRol,
                            rolJugador.Descripcion
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ERROR EN REGISTRO ===");
                Console.WriteLine($"Tipo: {ex.GetType().Name}");
                Console.WriteLine($"Mensaje: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }

                return BadRequest(new { success = false, message = $"Error en el registro: {ex.Message}" });
            }
        }

        // POST: api/auth/registro-arbitro
        [HttpPost("registro-arbitro")]
        [AllowAnonymous]
        public async Task<IActionResult> RegistroArbitro([FromBody] RegistroArbitroRequest request)
        {
            try
            {
                // Validar token
                var partes = request.Token.Split('|');
                if (partes[0] != "ARBITRO")
                    return BadRequest(new { success = false, message = "Token inválido para registro de árbitro" });

                var fechaExpiracion = DateTime.Parse(partes[1]);
                if (fechaExpiracion < DateTime.Now)
                    return BadRequest(new { success = false, message = "Token expirado" });

                // Verificar email
                if (await _context.Usuarios.AnyAsync(u => u.Email == request.Email))
                    return BadRequest(new { success = false, message = "El email ya está registrado" });

                // Crear usuario
                var usuario = new Usuario
                {
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Nombre = request.Nombre,
                    Apellidos = "",
                    Telefono = request.Telefono,
                    Activo = true
                };
                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                // Asignar rol Arbitro
                var rolArbitro = await _context.Roles.FirstOrDefaultAsync(r => r.NombreRol == "Arbitro");
                if (rolArbitro == null)
                {
                    rolArbitro = new Rol
                    {
                        NombreRol = "Arbitro",
                        Descripcion = "Árbitro de partidos"
                    };
                    _context.Roles.Add(rolArbitro);
                    await _context.SaveChangesAsync();
                }

                _context.UsuarioRoles.Add(new UsuarioRol
                {
                    UsuarioID = usuario.UsuarioID,
                    RolID = rolArbitro.RolID
                });
                await _context.SaveChangesAsync();

                // Generar token JWT
                var roles = new List<string> { "Arbitro" };
                var token = GenerarToken(usuario, roles);

                return Ok(new
                {
                    success = true,
                    message = "Registro exitoso",
                    data = new
                    {
                        token,
                        usuario = new
                        {
                            usuario.UsuarioID,
                            usuario.Email,
                            usuario.Nombre,
                            usuario.Telefono,
                            usuario.FechaRegistro,
                            usuario.Activo,
                            rolID = rolArbitro.RolID
                        },
                        rol = new
                        {
                            rolArbitro.RolID,
                            rolArbitro.NombreRol,
                            rolArbitro.Descripcion
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Error en el registro: {ex.Message}" });
            }
        }
        // POST: api/auth/generar-qr-capitan
        [HttpPost("generar-qr-capitan")]
        [AllowAnonymous] // ⚠️ Solo para desarrollo/testing
        public IActionResult GenerarQRCapitan()
        {
            var invitacionID = new Random().Next(100000, 999999);
            var expiracion = DateTime.Now.AddDays(7).ToString("yyyy-MM-ddTHH:mm:ss");
            var token = $"CAPITAN|{expiracion}|0|{invitacionID}";

            return Ok(new
            {
                success = true,
                data = token,
                message = "Código QR generado exitosamente",
                expiraEn = "7 días"
            });
        }

        //// POST: api/auth/generar-qr-capitan
        //[HttpPost("generar-qr-capitan")]
        //[Authorize(Roles = "Administrador")]
        //public IActionResult GenerarQRCapitan()
        //{
        //    var invitacionID = new Random().Next(100000, 999999);
        //    var expiracion = DateTime.Now.AddDays(7).ToString("yyyy-MM-ddTHH:mm:ss");
        //    var token = $"CAPITAN|{expiracion}|0|{invitacionID}";

        //    return Ok(new
        //    {
        //        success = true,
        //        data = token,
        //        message = "Código QR generado exitosamente",
        //        expiraEn = "7 días"
        //    });
        //}
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

    public class RegistroCapitanRequest
    {
        public string Token { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string? Telefono { get; set; }
        public string NombreEquipo { get; set; }
        public string? Logo { get; set; }
    }

    public class RegistroJugadorRequest
    {
        public string Token { get; set; }
        public int EquipoID { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string? Telefono { get; set; }
        public int NumeroCamiseta { get; set; }
        public string Posicion { get; set; }
    }

    public class RegistroArbitroRequest
    {
        public string Token { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string? Telefono { get; set; }
        public string Licencia { get; set; }
    }
}