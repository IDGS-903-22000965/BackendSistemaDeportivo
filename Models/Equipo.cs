using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TorneoManager.API.Models
{
    [Table("Equipos")]
    public class Equipo
    {
        [Key]
        public int EquipoID { get; set; }
        public string NombreEquipo { get; set; }
        public string? LogoURL { get; set; }
        public string? ColorUniformePrimario { get; set; }
        public string? ColorUniformeSecundario { get; set; }
        public int CapitanID { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
        public bool Activo { get; set; } = true;
    }
}