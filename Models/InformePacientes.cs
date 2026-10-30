namespace mvc.Models
{
  // fila del informe de pacientes por obra social
  public class FilaPorObraSocial
  {
    public string obra_social { get; set; } = string.Empty;
    public int cantidad_pacientes { get; set; }
  }

  // fila del informe de pacientes con mas turnos
  public class FilaMasTurnos
  {
    public int paciente_id { get; set; }
    public string apellido { get; set; } = string.Empty;
    public string nombre { get; set; } = string.Empty;
    public string dni { get; set; } = string.Empty;
    public int cantidad_turnos { get; set; }
    public DateOnly ultimo_turno { get; set; }
  }
}