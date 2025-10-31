using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TorneoManager.API.Models
{
    [Table("Canchas")]
    public class Cancha
    {
        [Key]
        public int CanchaID { get; set; }
        public string NombreCancha { get; set; }
        public string? Direccion { get; set; }
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public int? Capacidad { get; set; }
        public string? TipoSuperficie { get; set; }
        public bool Activo { get; set; } = true;
    }
}