
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mvc.Models
{
    [Table("usuario")]
    public class Usuario
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
        [StringLength(150)]
        public string email { get; set; } = string.Empty;

        [Column("password_hash")]
        [Required]
        [StringLength(300)]
        public string password_hash { get; set; } = string.Empty;

        [Required]
        public Rol rol { get; set; }

        [StringLength(300)]
        public string? avatar_path { get; set; }

        [Column("profesional_id")]
        public int? profesional_id { get; set; }

        [ForeignKey(nameof(profesional_id))]
        public Profesional? Profesional { get; set; }

        public bool activo { get; set; } = true;

        [Column("fecha_creacion", TypeName = "timestamp without time zone")]
        public DateTime fecha_creacion { get; set; }

        [Column("fecha_ultimo_acceso", TypeName = "timestamp without time zone")]
        public DateTime? fecha_ultimo_acceso { get; set; }
    }
}
