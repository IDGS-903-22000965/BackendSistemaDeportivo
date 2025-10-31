using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TorneoManager.API.Models
{
    [Table("Pagos")]
    public class Pago
    {
        [Key]
        public int PagoID { get; set; }
        public int UsuarioID { get; set; }
        public int? TorneoID { get; set; }
        public int? PartidoID { get; set; }
        public string TipoPago { get; set; }
        public decimal Monto { get; set; }
        public string? MetodoPago { get; set; }
        public string EstatusPago { get; set; } = "Completado";
        public string? ReferenciaPago { get; set; }
        public DateTime FechaPago { get; set; } = DateTime.Now;
        public string? ComprobanteURL { get; set; }
    }
}