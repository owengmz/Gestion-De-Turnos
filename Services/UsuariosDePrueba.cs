using Microsoft.AspNetCore.Identity;
using mvc.Models;

namespace mvc.Services;

// siembra usuarios por rol (idempotente)
public static class UsuariosDePrueba
{
  private const string DniProfesional = "30111001";

  public static void Sembrar(AppDbContext db, IPasswordHasher<Usuario> hasher)
  {
    Crear(db, hasher, "Admin", "Sistema", "admin@admin.com",
        "admin123", Rol.Administrador, null);

    Crear(db, hasher, "Rita", "Recepcion", "recepcion@clinica.com",
        "recepcion123", Rol.Recepcionista, null);

    // el profesional se busca por dni; si no existe se omite
    var profesional = db.Profesionales.FirstOrDefault(p => p.dni == DniProfesional);
    if (profesional is not null)
    {
      Crear(db, hasher, "Laura", "Fernandez", "profesional@clinica.com",
          "profesional123", Rol.Profesional, profesional.id);
    }
  }

  private static void Crear(AppDbContext db, IPasswordHasher<Usuario> hasher,
      string nombre, string apellido, string email, string clave,
      Rol rol, int? profesionalId)
  {
    // si el email ya existe no se crea de nuevo
    if (db.Usuarios.Any(u => u.email.ToLower() == email))
    {
      return;
    }

    var usuario = new Usuario
    {
      nombre = nombre,
      apellido = apellido,
      email = email,
      rol = rol,
      activo = true,
      profesional_id = profesionalId,
      fecha_creacion = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
    };
    usuario.password_hash = hasher.HashPassword(usuario, clave);

    db.Usuarios.Add(usuario);
    db.SaveChanges();
  }
}