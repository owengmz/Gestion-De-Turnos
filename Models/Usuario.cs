
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
        public string password_hash { get; set; } = string.Empty;

        public string rol { get; set; } = string.Empty;

        public bool activo { get; set; } = true;
    }
}
