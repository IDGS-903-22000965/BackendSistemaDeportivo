using Microsoft.EntityFrameworkCore;
using TorneoManager.API.Models;

namespace TorneoManager.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<UsuarioRol> UsuarioRoles { get; set; }
        public DbSet<Equipo> Equipos { get; set; }
        public DbSet<Jugador> Jugadores { get; set; }
        public DbSet<DocumentoJugador> DocumentosJugador { get; set; }
        public DbSet<Torneo> Torneos { get; set; }
        public DbSet<ReglaTorneo> ReglasTorneo { get; set; }
        public DbSet<InscripcionTorneo> InscripcionesTorneo { get; set; }
        public DbSet<Cancha> Canchas { get; set; }
        public DbSet<Partido> Partidos { get; set; }
        public DbSet<EventoPartido> EventosPartido { get; set; }
        public DbSet<IncidentePartido> IncidentesPartido { get; set; }
        public DbSet<Sancion> Sanciones { get; set; }
        public DbSet<Pago> Pagos { get; set; }
        public DbSet<Notificacion> Notificaciones { get; set; }
        public DbSet<CodigoQR> CodigosQR { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UsuarioRol>().HasKey(ur => new { ur.UsuarioID, ur.RolID });
        }
    }
}