using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace mvc.Models;

[Table("especialidad")]
public class Especialidad
{
    [Key]
    public int id { get; set; }

    [Display(Name = "Nombre")]
    [Required(ErrorMessage = "El nombre de la especialidad es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string nombre { get; set; } = string.Empty;

    [Display(Name = "Descripción")]
    [StringLength(300, ErrorMessage = "La descripción no puede superar los 300 caracteres.")]
    public string? descripcion { get; set; }

    [Display(Name = "Estado")]
    public bool estado { get; set; } = true;

    [ValidateNever]
    public ICollection<Profesional> Profesionales { get; set; } = new List<Profesional>();
}
