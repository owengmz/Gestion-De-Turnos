
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mvc.Models
{
    [Table("turno")]
    public class Turno
    {
        [Key]
        public int id { get; set; }

        [Required]
        public DateOnly fecha { get; set; }

        [Required]
        public TimeOnly hora { get; set; }

        [StringLength(300)]
        public string? motivo { get; set; }

        [Required]
        [StringLength(20)]
        public string estado { get; set; } = "Pendiente";

        [StringLength(500)]
        public string? observaciones { get; set; }

        [Column("paciente_id")]
        public int paciente_id { get; set; }
        [ForeignKey("paciente_id")]
        public Paciente? Paciente { get; set; }

        [Column("profesional_id")]
        public int profesional_id { get; set; }
        [ForeignKey("profesional_id")]
        public Profesional? Profesional { get; set; }

        [Column("usuario_creador_id")]
        public int usuario_creador_id { get; set; }

        [Column("usuario_modificador_id")]
        public int? usuario_modificador_id { get; set; }

        [Column("fecha_creacion")]
        public DateTime fecha_creacion { get; set; }

        [Column("fecha_modificacion")]
        public DateTime? fecha_modificacion { get; set; }
    }
}
