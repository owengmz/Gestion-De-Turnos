using Microsoft.EntityFrameworkCore;

namespace mvc.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Turno> Turnos { get; set; }
        public DbSet<HorarioAtencion> HorariosAtencion { get; set; }
    }
}