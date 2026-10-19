using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mvc.Models;

[Table("especialidad")]
public class Especialidad
{
    [Key]
    public int id { get; set; }

    [Required]
    [StringLength(100)]
    public string nombre { get; set; } = string.Empty;

    [StringLength(300)]
    public string? descripcion { get; set; }

    public bool estado { get; set; } = true;

    public ICollection<Profesional> Profesionales { get; set; } = new List<Profesional>();
}
