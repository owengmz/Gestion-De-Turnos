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
        public DbSet<Paciente> Pacientes { get; set; }
        public DbSet<Profesional> Profesionales { get; set; }
        public DbSet<Especialidad> Especialidades { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasIndex(usuario => usuario.email).IsUnique();

                entity.Property(usuario => usuario.activo)
                    .HasDefaultValue(true);

                entity.Property(usuario => usuario.fecha_creacion)
                    .HasDefaultValueSql("now()");

                entity.HasOne(usuario => usuario.Profesional)
                    .WithMany(profesional => profesional.Usuarios)
                    .HasForeignKey(usuario => usuario.profesional_id)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("fk_usuario_profesional");
            });

            modelBuilder.Entity<Especialidad>()
                .Property(especialidad => especialidad.estado)
                .HasDefaultValue(true);

            modelBuilder.Entity<Profesional>(entity =>
            {
                entity.HasIndex(profesional => profesional.dni).IsUnique();
                entity.HasIndex(profesional => profesional.matricula).IsUnique();

                entity.Property(profesional => profesional.estado)
                    .HasDefaultValue(true);

                entity.HasOne(profesional => profesional.Especialidad)
                    .WithMany(especialidad => especialidad.Profesionales)
                    .HasForeignKey(profesional => profesional.especialidad_id)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
