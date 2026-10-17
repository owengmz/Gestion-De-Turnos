
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

        public bool estado { get; set; } = true;

        public ICollection<Turno> Turnos { get; set; } = new List<Turno>();

        public ICollection<HorarioAtencion> HorariosAtencion { get; set; }
            = new List<HorarioAtencion>();
    }
}
