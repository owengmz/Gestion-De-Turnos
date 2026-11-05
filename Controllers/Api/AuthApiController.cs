using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using mvc.Models;
using mvc.Models.DTOs;

namespace mvc.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
  private readonly AppDbContext _context;
  private readonly IPasswordHasher<Usuario> _passwordHasher;
  private readonly IConfiguration _configuration;

  public AuthApiController(
      AppDbContext context,
      IPasswordHasher<Usuario> passwordHasher,
      IConfiguration configuration)
  {
    _context = context;
    _passwordHasher = passwordHasher;
    _configuration = configuration;
  }

  // POST: /api/auth/login
  [HttpPost("login")]
  public async Task<IActionResult> Login([FromForm] LoginApiDto model)
  {
    if (!ModelState.IsValid)
    {
      return BadRequest(ModelState);
    }

    var emailNormalizado = model.Email.Trim().ToLower();

    var usuario = await _context.Usuarios
        .Include(u => u.Profesional)
        .FirstOrDefaultAsync(u => u.email.ToLower() == emailNormalizado);

    if (usuario == null)
    {
      return Unauthorized(new { mensaje = "Credenciales inválidas o usuario inexistente." });
    }

    if (!usuario.activo)
    {
      return Unauthorized(new { mensaje = "La cuenta de usuario se encuentra desactivada." });
    }

    // Verificar la contraseña con PasswordHasher
    var resultado = _passwordHasher.VerifyHashedPassword(usuario, usuario.password_hash, model.Password);
    if (resultado == PasswordVerificationResult.Failed)
    {
      return Unauthorized(new { mensaje = "Credenciales inválidas o usuario inexistente." });
    }

    // Registrar último acceso
    usuario.fecha_ultimo_acceso = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
    await _context.SaveChangesAsync();

    // Generar Token JWT firmado
    var token = GenerarTokenJwt(usuario, out DateTime fechaExpiracion);

    return Ok(new AuthResponseDto
    {
      Token = token,
      TokenType = "Bearer",
      Expiracion = fechaExpiracion,
      UsuarioId = usuario.id,
      Email = usuario.email,
      NombreCompleto = $"{usuario.apellido}, {usuario.nombre}",
      Rol = usuario.rol.ToString(),
      ProfesionalId = usuario.profesional_id
    });
  }

  private string GenerarTokenJwt(Usuario usuario, out DateTime fechaExpiracion)
  {
    var jwtKey = _configuration["Jwt:Key"] ?? "ClaveSecretaSuperSeguraParaCentroMedicoTurnos2026!*";
    var issuer = _configuration["Jwt:Issuer"] ?? "GestionDeTurnosAPI";
    var audience = _configuration["Jwt:Audience"] ?? "GestionDeTurnosClients";
    var duracionHoras = int.TryParse(_configuration["Jwt:DurationInHours"], out var h) ? h : 8;

    fechaExpiracion = DateTime.UtcNow.AddHours(duracionHoras);

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
    var credenciales = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.id.ToString()),
            new Claim(ClaimTypes.Email, usuario.email),
            new Claim(ClaimTypes.Name, $"{usuario.nombre} {usuario.apellido}"),
            new Claim(ClaimTypes.Role, usuario.rol.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

    if (usuario.profesional_id.HasValue)
    {
      claims.Add(new Claim("ProfesionalId", usuario.profesional_id.Value.ToString()));
    }

    var tokenDescriptor = new SecurityTokenDescriptor
    {
      Subject = new ClaimsIdentity(claims),
      Expires = fechaExpiracion,
      Issuer = issuer,
      Audience = audience,
      SigningCredentials = credenciales
    };

    var tokenHandler = new JwtSecurityTokenHandler();
    var securityToken = tokenHandler.CreateToken(tokenDescriptor);

    return tokenHandler.WriteToken(securityToken);
  }
}

