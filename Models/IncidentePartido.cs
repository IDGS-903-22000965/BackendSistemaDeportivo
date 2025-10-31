using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TorneoManager.API.Models
{
    [Table("IncidentesPartido")]
    public class IncidentePartido
    {
        [Key]
        public int IncidenteID { get; set; }
        public int PartidoID { get; set; }
        public int ArbitroID { get; set; }
        public string TipoIncidente { get; set; }
        public string Descripcion { get; set; }
        public string? Gravedad { get; set; }
        public DateTime FechaHoraIncidente { get; set; } = DateTime.Now;
        public string? EvidenciaURL { get; set; }
    }
}