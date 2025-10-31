using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TorneoManager.API.Models
{
    [Table("InscripcionesTorneo")]
    public class InscripcionTorneo
    {
        [Key]
        public int InscripcionID { get; set; }
        public int TorneoID { get; set; }
        public int EquipoID { get; set; }
        public DateTime FechaInscripcion { get; set; } = DateTime.Now;
        public string EstatusPago { get; set; } = "Pendiente";
        public DateTime? FechaPago { get; set; }
        public decimal? MontoTotal { get; set; }
    }
}