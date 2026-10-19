using System.ComponentModel.DataAnnotations;

namespace mvc.Models.ViewModels;

public sealed class PerfilViewModel
{
    public string NombreCompleto { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? AvatarPath { get; set; }

    [Display(Name = "Imagen de perfil")]
    public IFormFile? Avatar { get; set; }
}
