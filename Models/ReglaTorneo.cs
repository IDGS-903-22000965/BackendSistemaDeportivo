using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TorneoManager.API.Models
{
    [Table("ReglasTorneo")]
    public class ReglaTorneo
    {
        [Key]
        public int ReglaID { get; set; }
        public int TorneoID { get; set; }
        public int PuntosPorVictoria { get; set; } = 3;
        public int PuntosPorEmpate { get; set; } = 1;
        public int PuntosPorDerrota { get; set; } = 0;
        public int TarjetasAmarillasParaSuspension { get; set; } = 2;
        public int PartidosSuspensionTarjetaRoja { get; set; } = 3;
        public int DuracionPartido { get; set; } = 90;
    }
}