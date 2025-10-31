using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TorneoManager.API.Models
{
    [Table("Torneos")]
    public class Torneo
    {
        [Key]
        public int TorneoID { get; set; }
        public string NombreTorneo { get; set; }
        public string? Categoria { get; set; }
        public string? TipoTorneo { get; set; }
        public decimal? CuotaInscripcion { get; set; }
        public decimal? CostoArbitraje { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public bool Activo { get; set; } = true;
        public int AdminID { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}