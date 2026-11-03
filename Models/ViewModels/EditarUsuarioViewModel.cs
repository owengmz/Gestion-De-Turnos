using System.ComponentModel.DataAnnotations;

namespace mvc.Models.ViewModels;

public class EditarUsuarioViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(100, ErrorMessage = "El apellido no puede superar los 100 caracteres.")]
    [Display(Name = "Apellido")]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
    [StringLength(150, ErrorMessage = "El email no puede superar los 150 caracteres.")]
    [Display(Name = "Correo Electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe seleccionar un rol.")]
    [Display(Name = "Rol en el Sistema")]
    public Rol Rol { get; set; }

    [Display(Name = "Profesional Médico")]
    public int? ProfesionalId { get; set; }

    [Display(Name = "Cuenta Activa")]
    public bool Activo { get; set; } = true;

    // Reseteo opcional de contraseña
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La nueva contraseña debe tener al menos 6 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva Contraseña (opcional)")]
    public string? PasswordNueva { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirmar Nueva Contraseña")]
    [Compare(nameof(PasswordNueva), ErrorMessage = "Las contraseñas no coinciden.")]
    public string? ConfirmarPasswordNueva { get; set; }

    // Bandera para indicar si es el usuario que tiene la sesión abierta actualmente
    public bool EsUsuarioLogueado { get; set; }
}

