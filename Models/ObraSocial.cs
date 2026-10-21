// dependencias (imports)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// paquete
namespace mvc.Models
{
  // atributo de clase: enlaza la clase con la tabla
  [Table("obra_social")]
  public class ObraSocial
  {
    // propiedades (atributo + getter + setter)
    [Key]
    public int id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres")]
    [Display(Name = "Nombre")]
    public string nombre { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "La descripcion no puede superar los 300 caracteres")]
    [Display(Name = "Descripcion")]
    public string? descripcion { get; set; }

    [Display(Name = "Activa")]
    public bool estado { get; set; } = true;

    // relacion 
    public ICollection<Paciente> Pacientes { get; set; } = new List<Paciente>();

  }
}