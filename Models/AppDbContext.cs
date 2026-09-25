using GestorTurnos.Models;
using Microsoft.EntityFrameworkCore;

namespace GestorTurnos.Data
{
  public class ApplicationDbContext : DbContext
  {
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Especialidad> Especialidades => Set<Especialidad>();
    public DbSet<Profesional> Profesionales => Set<Profesional>();
    public DbSet<Consultorio> Consultorios => Set<Consultorio>();
    public DbSet<ObraSocial> ObrasSociales => Set<ObraSocial>();
    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<HorarioAtencion> HorariosAtencion => Set<HorarioAtencion>();
    public DbSet<Turno> Turnos => Set<Turno>();
    public DbSet<Pago> Pagos => Set<Pago>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
      base.OnModelCreating(builder);

      // Tabla intermedia usuario_rol (muchos a muchos)
      builder.Entity<Usuario>()
          .HasMany(u => u.Roles)
          .WithMany(r => r.Usuarios)
          .UsingEntity(j => j.ToTable("usuario_rol"));

      // Enums guardados como string en la base (convencion del equipo)
      builder.Entity<Turno>()
          .Property(t => t.Estado)
          .HasConversion<string>();

      builder.Entity<Pago>()
          .Property(p => p.Estado)
          .HasConversion<string>();

      builder.Entity<HorarioAtencion>()
          .Property(h => h.DiaSemana)
          .HasConversion<string>();

      // Fechas de auditoria generadas por la base, nunca desde C#
      builder.Entity<Usuario>()
          .Property(u => u.FechaCreacion)
          .HasDefaultValueSql("now()");

      builder.Entity<Turno>()
          .Property(t => t.FechaCreacion)
          .HasDefaultValueSql("now()");

      builder.Entity<Pago>()
          .Property(p => p.Fecha)
          .HasDefaultValueSql("now()");

      // Paciente/ObraSocial: al desactivar la obra social no se pierde
      // la relacion historica, por eso el FK es opcional (int?) y con
      // Restrict en vez de Cascade
      builder.Entity<Paciente>()
          .HasOne(pa => pa.ObraSocial)
          .WithMany(o => o.Pacientes)
          .HasForeignKey(pa => pa.ObraSocialId)
          .OnDelete(DeleteBehavior.Restrict);

      // Turno: no se debe perder el registro si se desactiva un
      // consultorio, profesional o paciente
      builder.Entity<Turno>()
          .HasOne(t => t.Consultorio)
          .WithMany(c => c.Turnos)
          .HasForeignKey(t => t.ConsultorioId)
          .OnDelete(DeleteBehavior.Restrict);

      builder.Entity<Turno>()
          .HasOne(t => t.Profesional)
          .WithMany(p => p.Turnos)
          .HasForeignKey(t => t.ProfesionalId)
          .OnDelete(DeleteBehavior.Restrict);

      builder.Entity<Turno>()
          .HasOne(t => t.Paciente)
          .WithMany(pa => pa.Turnos)
          .HasForeignKey(t => t.PacienteId)
          .OnDelete(DeleteBehavior.Restrict);

      // Auditoria de Turno: dos FK distintas a Usuario, hay que
      // indicarle a EF cual es cual para que no naveguen a la
      // misma coleccion por defecto
      builder.Entity<Turno>()
          .HasOne(t => t.UsuarioCreador)
          .WithMany(u => u.TurnosCreados)
          .HasForeignKey(t => t.UsuarioCreadorId)
          .OnDelete(DeleteBehavior.Restrict);

      builder.Entity<Turno>()
          .HasOne(t => t.UsuarioModificador)
          .WithMany(u => u.TurnosModificados)
          .HasForeignKey(t => t.UsuarioModificadorId)
          .OnDelete(DeleteBehavior.Restrict);

      // Auditoria de Pago: mismo criterio, creador y anulador son
      // dos relaciones distintas hacia Usuario
      builder.Entity<Pago>()
          .HasOne(p => p.UsuarioCreador)
          .WithMany(u => u.PagosCreados)
          .HasForeignKey(p => p.UsuarioCreadorId)
          .OnDelete(DeleteBehavior.Restrict);

      builder.Entity<Pago>()
          .HasOne(p => p.UsuarioAnulador)
          .WithMany(u => u.PagosAnulados)
          .HasForeignKey(p => p.UsuarioAnuladorId)
          .OnDelete(DeleteBehavior.Restrict);
    }
  }
}