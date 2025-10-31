using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TorneoManager.API.Models
{
    [Table("Usuarios")]
    public class Usuario
    {
        [Key]
        public int UsuarioID { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Nombre { get; set; }
        public string Apellidos { get; set; }
        public string? Telefono { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
        public DateTime? UltimoAcceso { get; set; }
        public bool Activo { get; set; } = true;
        public string? TokenDispositivo { get; set; }
    }
}