using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace mvc.Models;

[Table("profesional")]
public class Profesional
{
    [Key]
    public int id { get; set; }

    [Display(Name = "Nombre")]
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string nombre { get; set; } = string.Empty;

    [Display(Name = "Apellido")]
    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(100, ErrorMessage = "El apellido no puede superar los 100 caracteres.")]
    public string apellido { get; set; } = string.Empty;

    [Display(Name = "DNI")]
    [Required(ErrorMessage = "El DNI es obligatorio.")]
    [StringLength(20, ErrorMessage = "El DNI no puede superar los 20 caracteres.")]
    public string dni { get; set; } = string.Empty;

    [Display(Name = "Matrícula")]
    [Required(ErrorMessage = "La matrícula profesional es obligatoria.")]
    [StringLength(50, ErrorMessage = "La matrícula no puede superar los 50 caracteres.")]
    public string matricula { get; set; } = string.Empty;

    [Display(Name = "Teléfono")]
    [StringLength(50, ErrorMessage = "El teléfono no puede superar los 50 caracteres.")]
    public string? telefono { get; set; }

    [Display(Name = "Correo Electrónico")]
    [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
    [StringLength(150, ErrorMessage = "El correo no puede superar los 150 caracteres.")]
    public string? email { get; set; }

    [Display(Name = "Estado")]
    public bool estado { get; set; } = true;

    [Display(Name = "Especialidad")]
    [Required(ErrorMessage = "Debe seleccionar una especialidad médica.")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una especialidad médica válida.")]
    [Column("especialidad_id")]
    public int especialidad_id { get; set; }

    [ForeignKey(nameof(especialidad_id))]
    [ValidateNever]
    public Especialidad? Especialidad { get; set; }

    [ValidateNever]
    public ICollection<Turno> Turnos { get; set; } = new List<Turno>();

    [ValidateNever]
    public ICollection<HorarioAtencion> HorariosAtencion { get; set; }
        = new List<HorarioAtencion>();

    [ValidateNever]
    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
