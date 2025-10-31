using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TorneoManager.API.Models
{
    [Table("Sanciones")]
    public class Sancion
    {
        [Key]
        public int SancionID { get; set; }
        public int JugadorID { get; set; }
        public int TorneoID { get; set; }
        public string TipoSancion { get; set; }
        public int PartidosSuspension { get; set; }
        public int PartidosCumplidos { get; set; } = 0;
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public bool Activa { get; set; } = true;
        public string? Motivo { get; set; }
        public int? EventoRelacionadoID { get; set; }
    }
}