using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TorneoManager.API.Models
{
    [Table("EventosPartido")]
    public class EventoPartido
    {
        [Key]
        public int EventoID { get; set; }
        public int PartidoID { get; set; }
        public int JugadorID { get; set; }
        public string TipoEvento { get; set; }
        public int? Minuto { get; set; }
        public int? AsistenciaJugadorID { get; set; }
        public string? Comentarios { get; set; }
        public DateTime FechaHoraEvento { get; set; } = DateTime.Now;
        // Sergio
        public virtual Jugador Jugador { get; set; }
    }
}