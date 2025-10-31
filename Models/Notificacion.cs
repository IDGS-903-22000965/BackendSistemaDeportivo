using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TorneoManager.API.Models
{
    [Table("Notificaciones")]
    public class Notificacion
    {
        [Key]
        public int NotificacionID { get; set; }
        public int UsuarioID { get; set; }
        public string Titulo { get; set; }
        public string Mensaje { get; set; }
        public string TipoNotificacion { get; set; }
        public bool Leida { get; set; } = false;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaEnvio { get; set; }
        public int? ReferenciaID { get; set; }
    }
}