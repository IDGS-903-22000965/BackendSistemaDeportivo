using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TorneoManager.API.Models
{
    [Table("CodigosQR")]
    public class CodigoQR
    {
        [Key]
        public int CodigoQRID { get; set; }
        public int TorneoID { get; set; }
        public string CodigoUnico { get; set; }
        public DateTime FechaGeneracion { get; set; } = DateTime.Now;
        public DateTime FechaExpiracion { get; set; }
        public bool Usado { get; set; } = false;
        public int? EquipoRegistradoID { get; set; }
        public DateTime? FechaUso { get; set; }
        public int GeneradoPorID { get; set; }
    }
}