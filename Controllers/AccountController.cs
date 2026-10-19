using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mvc.Models;
using mvc.Models.ViewModels;
using mvc.Services;

namespace mvc.Controllers;

public sealed class AccountController : Controller
{
    private const string InvalidCredentialsMessage = "Correo o contraseña incorrectos.";
    private const string ProfessionalIdClaim = "ProfesionalId";
    private const string AvatarPathClaim = "AvatarPath";

    private readonly AppDbContext _context;
    private readonly IPasswordHasher<Usuario> _passwordHasher;
    private readonly AvatarStorageService _avatarStorage;

    public AccountController(
        AppDbContext context,
        IPasswordHasher<Usuario> passwordHasher,
        AvatarStorageService avatarStorage)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _avatarStorage = avatarStorage;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel
        {
            ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null
        });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        var matchingUsers = await _context.Usuarios
            .Where(user => user.email.ToLower() == normalizedEmail)
            .Take(2)
            .ToListAsync();
        var user = matchingUsers.Count == 1 ? matchingUsers[0] : null;

        if (user is null || !user.activo)
        {
            ModelState.AddModelError(string.Empty, InvalidCredentialsMessage);
            return View(model);
        }

        PasswordVerificationResult verification;
        try
        {
            verification = _passwordHasher.VerifyHashedPassword(
                user,
                user.password_hash,
                model.Password);
        }
        catch (FormatException)
        {
            ModelState.AddModelError(string.Empty, InvalidCredentialsMessage);
            return View(model);
        }

        if (verification == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, InvalidCredentialsMessage);
            return View(model);
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.password_hash = _passwordHasher.HashPassword(user, model.Password);
        }

        user.fecha_ultimo_acceso = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        await _context.SaveChangesAsync();

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            CreatePrincipal(user),
            new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                AllowRefresh = true
            });

        return RedirectToLocal(model.ReturnUrl);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Perfil()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return Forbid();
        }

        return View(ToPerfilViewModel(user));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(2 * 1024 * 1024 + 64 * 1024)]
    public async Task<IActionResult> Perfil(PerfilViewModel model)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return Forbid();
        }

        var validationError = await _avatarStorage.ValidateAsync(model.Avatar);
        if (validationError is not null)
        {
            ModelState.AddModelError(nameof(model.Avatar), validationError);
            return View(ToPerfilViewModel(user));
        }

        var previousAvatarPath = user.avatar_path;
        var newAvatarPath = await _avatarStorage.SaveAsync(user.id, model.Avatar!);
        user.avatar_path = newAvatarPath;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch
        {
            user.avatar_path = previousAvatarPath;
            _avatarStorage.DeleteIfManaged(newAvatarPath);
            throw;
        }

        _avatarStorage.DeleteIfManaged(previousAvatarPath);
        await RefreshAuthenticationCookieAsync(user);
        TempData["ProfileMessage"] = "Tu imagen de perfil se actualizó.";

        return RedirectToAction(nameof(Perfil));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private async Task<Usuario?> GetCurrentUserAsync()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdValue, out var userId)
            ? await _context.Usuarios.SingleOrDefaultAsync(user => user.id == userId)
            : null;
    }

    private async Task RefreshAuthenticationCookieAsync(Usuario user)
    {
        var currentAuthentication = await HttpContext.AuthenticateAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            CreatePrincipal(user),
            currentAuthentication.Properties ?? new AuthenticationProperties());
    }

    private static ClaimsPrincipal CreatePrincipal(Usuario user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.id.ToString()),
            new(ClaimTypes.Name, $"{user.nombre} {user.apellido}".Trim()),
            new(ClaimTypes.Email, user.email),
            new(ClaimTypes.Role, user.rol.ToString())
        };

        if (!string.IsNullOrWhiteSpace(user.avatar_path))
        {
            claims.Add(new Claim(AvatarPathClaim, user.avatar_path));
        }

        if (user.profesional_id.HasValue)
        {
            claims.Add(new Claim(ProfessionalIdClaim, user.profesional_id.Value.ToString()));
        }

        return new ClaimsPrincipal(
            new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    private PerfilViewModel ToPerfilViewModel(Usuario user)
        => new()
        {
            NombreCompleto = $"{user.nombre} {user.apellido}".Trim(),
            Email = user.email,
            AvatarPath = user.avatar_path
        };

    private IActionResult RedirectToLocal(string? returnUrl)
        => Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl!)
            : RedirectToAction("Index", "Home")!;
}
