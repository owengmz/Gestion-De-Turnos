
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mvc.Models
{
    [Table("horario_atencion")]
    public class HorarioAtencion
    {
        [Key]
        public int id { get; set; }

        [Required]
        [StringLength(20)]
        public string dia_semana { get; set; } = string.Empty;

        [Required]
        public TimeOnly hora_desde { get; set; }

        [Required]
        public TimeOnly hora_hasta { get; set; }

        public bool estado { get; set; } = true;

        [Column("profesional_id")]
        public int profesional_id { get; set; }

        [ForeignKey("profesional_id")]
        public Profesional? Profesional { get; set; }
    }
}
