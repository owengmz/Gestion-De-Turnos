using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using mvc.Models;
using mvc.Models.ViewModels;

namespace mvc.Controllers;

[Authorize(Roles = "Administrador")]
public class UsuariosController : Controller
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher<Usuario> _passwordHasher;

    public UsuariosController(AppDbContext context, IPasswordHasher<Usuario> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    // GET: Usuarios
    public async Task<IActionResult> Index(string? search, Rol? rolFiltro, string? estadoFiltro)
    {
        var query = _context.Usuarios
            .Include(u => u.Profesional)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(u =>
                u.nombre.ToLower().Contains(searchLower) ||
                u.apellido.ToLower().Contains(searchLower) ||
                u.email.ToLower().Contains(searchLower));
        }

        if (rolFiltro.HasValue)
        {
            query = query.Where(u => u.rol == rolFiltro.Value);
        }

        if (estadoFiltro == "activos")
        {
            query = query.Where(u => u.activo);
        }
        else if (estadoFiltro == "inactivos")
        {
            query = query.Where(u => !u.activo);
        }

        var usuarios = await query
            .OrderBy(u => u.apellido)
            .ThenBy(u => u.nombre)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.RolFiltro = rolFiltro;
        ViewBag.EstadoFiltro = estadoFiltro ?? "todos";

        return View(usuarios);
    }

    // GET: Usuarios/Create
    public async Task<IActionResult> Create()
    {
        await CargarComboProfesionalesAsync();
        return View(new CrearUsuarioViewModel());
    }

    // POST: Usuarios/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CrearUsuarioViewModel model)
    {
        // 1. Validar unicidad del email
        if (!string.IsNullOrWhiteSpace(model.Email))
        {
            var emailNormalizado = model.Email.Trim().ToLower();
            var emailExiste = await _context.Usuarios
                .AnyAsync(u => u.email.ToLower() == emailNormalizado);

            if (emailExiste)
            {
                ModelState.AddModelError(nameof(model.Email), "Ya existe un usuario registrado con este correo electrónico.");
            }
        }

        // 2. Si el rol es Profesional, validar que se seleccione un profesional médico existente
        if (model.Rol == Rol.Profesional)
        {
            if (!model.ProfesionalId.HasValue || model.ProfesionalId.Value <= 0)
            {
                ModelState.AddModelError(nameof(model.ProfesionalId), "Debe seleccionar el profesional médico al que pertenecerá esta cuenta.");
            }
            else
            {
                var profExiste = await _context.Profesionales.AnyAsync(p => p.id == model.ProfesionalId.Value);
                if (!profExiste)
                {
                    ModelState.AddModelError(nameof(model.ProfesionalId), "El profesional médico seleccionado no existe.");
                }
            }
        }

        if (!ModelState.IsValid)
        {
            await CargarComboProfesionalesAsync(model.ProfesionalId);
            return View(model);
        }

        var usuario = new Usuario
        {
            nombre = model.Nombre.Trim(),
            apellido = model.Apellido.Trim(),
            email = model.Email.Trim().ToLower(),
            rol = model.Rol,
            profesional_id = model.Rol == Rol.Profesional ? model.ProfesionalId : null,
            activo = true,
            fecha_creacion = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
        };

        // Hashing criptográfico seguro con sal por usuario (PBKDF2)
        usuario.password_hash = _passwordHasher.HashPassword(usuario, model.Password);

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Usuario {usuario.email} ({usuario.rol}) registrado exitosamente.";
        return RedirectToAction(nameof(Index));
    }

    private async Task CargarComboProfesionalesAsync(int? seleccionado = null)
    {
        var profesionales = await _context.Profesionales
            .Where(p => p.estado || p.id == seleccionado)
            .OrderBy(p => p.apellido)
            .ThenBy(p => p.nombre)
            .Select(p => new
            {
                Id = p.id,
                NombreCompleto = $"{p.apellido}, {p.nombre} (Mat. {p.matricula})"
            })
            .ToListAsync();

        ViewBag.Profesionales = new SelectList(profesionales, "Id", "NombreCompleto", seleccionado);
    }
}

