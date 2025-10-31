using System.ComponentModel.DataAnnotations.Schema;

namespace TorneoManager.API.Models
{
    [Table("UsuarioRoles")]
    public class UsuarioRol
    {
        public int UsuarioID { get; set; }
        public int RolID { get; set; }
        public DateTime FechaAsignacion { get; set; } = DateTime.Now;
    }
}