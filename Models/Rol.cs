using NpgsqlTypes;

namespace mvc.Models;

public enum Rol
{
    [PgName("Administrador")]
    Administrador = 1,

    [PgName("Recepcionista")]
    Recepcionista = 2,

    [PgName("Profesional")]
    Profesional = 3
}
