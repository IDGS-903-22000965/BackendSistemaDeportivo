using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TorneoManager.API.Models
{
    [Table("DocumentosJugador")]
    public class DocumentoJugador
    {
        [Key]
        public int DocumentoID { get; set; }
        public int JugadorID { get; set; }
        public string TipoDocumento { get; set; }
        public string NombreArchivo { get; set; }
        public string URLArchivo { get; set; }
        public DateTime FechaSubida { get; set; } = DateTime.Now;
        public string EstatusValidacion { get; set; } = "Pendiente";
        public string? ComentariosValidacion { get; set; }
        public DateTime? FechaValidacion { get; set; }
    }
}