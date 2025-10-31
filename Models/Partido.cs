using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TorneoManager.API.Models
{
    [Table("Partidos")]
    public class Partido
    {
        [Key]
        public int PartidoID { get; set; }
        public int TorneoID { get; set; }
        public int EquipoLocalID { get; set; }
        public int EquipoVisitanteID { get; set; }
        public int? CanchaID { get; set; }
        public int? ArbitroID { get; set; }
        public DateTime FechaHora { get; set; }
        public int? Jornada { get; set; }
        public int GolesLocal { get; set; } = 0;
        public int GolesVisitante { get; set; } = 0;
        public string Estatus { get; set; } = "Programado";
        public string EstatusPagoArbitraje { get; set; } = "Pendiente";
        public DateTime? FechaPagoArbitraje { get; set; }
        public DateTime? FechaFinalizacion { get; set; }
    }
}