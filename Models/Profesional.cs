
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mvc.Models
{
    [Table("profesional")]
    public class Profesional
    {
        [Key]
        public int id { get; set; }

        [Required]
        [StringLength(100)]
        public string nombre { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string apellido { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string dni { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string matricula { get; set; } = string.Empty;

        [StringLength(50)]
        public string? telefono { get; set; }

        [StringLength(150)]
        public string? email { get; set; }

        public bool estado { get; set; } = true;

        [Column("especialidad_id")]
        public int especialidad_id { get; set; }

        [ForeignKey(nameof(especialidad_id))]
        public Especialidad? Especialidad { get; set; }

        public ICollection<Turno> Turnos { get; set; } = new List<Turno>();

        public ICollection<HorarioAtencion> HorariosAtencion { get; set; }
            = new List<HorarioAtencion>();

        public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }
}
