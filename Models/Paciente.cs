// dependencias (imports)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// paquete
namespace mvc.Models
{
  // atributo de clase: enlaza la clase con la tabla paciente
  [Table("paciente")]
  public class Paciente
  {
    // propiedades obligatorias (campo + getter + setter)
    [Key]
    public int id { get; set; }

    [Required]
    [StringLength(20)]
    public string dni { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string nombre { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string apellido { get; set; } = string.Empty;

    // propiedades opcionales (el ? permite null, igual que la base)
    [Display(Name = "Fecha de nacimiento")]
    [DataType(DataType.Date)]
    public DateOnly? fecha_nacimiento { get; set; }

    [StringLength(50)]
    [Display(Name = "Telefono")]
    public string? telefono { get; set; }

    [EmailAddress(ErrorMessage = "El formato del email no es valido")]
    [StringLength(150)]
    [Display(Name = "Email")]
    public string? email { get; set; }

    [StringLength(300)]
    [Display(Name = "Direccion")]
    public string? direccion { get; set; }

    [StringLength(50)]
    [Display(Name = "Numero de afiliado")]
    public string? numero_afiliado { get; set; }

    // ruta de la foto del carnet de la obra social
    [StringLength(300)]
    public string? credencial_path { get; set; }

    // estado: true = activo, false = desactivado
    [Display(Name = "Activo")]
    public bool estado { get; set; } = true;

    // clave foranea: es int? porque un paciente puede no tener obra social
    [Display(Name = "Obra social")]
    public int? obra_social_id { get; set; }

    // relacion N:1: la obra social de este paciente
    [ForeignKey("obra_social_id")]
    public ObraSocial? ObraSocial { get; set; }

    // relacion 1:N: lista de turnos de este paciente
    public ICollection<Turno> Turnos { get; set; } = new List<Turno>();
  }
}