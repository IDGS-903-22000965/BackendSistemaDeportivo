using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TorneoManager.API.Models
{
    [Table("Jugadores")]
    public class Jugador
    {
        [Key]
        public int JugadorID { get; set; }
        public int UsuarioID { get; set; }
        public int EquipoID { get; set; }
        public int? NumeroJugador { get; set; }
        public string? Posicion { get; set; }
        public string? FotoURL { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string Estatus { get; set; } = "Activo";
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }
}