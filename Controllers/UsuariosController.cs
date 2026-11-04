using System.Security.Claims;
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
    private const int RegistrosPorPagina = 3;

    public UsuariosController(AppDbContext context, IPasswordHasher<Usuario> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    // GET: Usuarios
    public async Task<IActionResult> Index(string? search, Rol? rolFiltro, string? estadoFiltro, int pagina = 1)
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

        query = query
            .OrderBy(u => u.apellido)
            .ThenBy(u => u.nombre);

        var usuarios = await ResultadoPaginado<Usuario>.CrearAsync(query, pagina, RegistrosPorPagina);

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

    // GET: Usuarios/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
        {
            return NotFound();
        }

        var usuarioActualIdTexto = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int.TryParse(usuarioActualIdTexto, out int usuarioActualId);

        var model = new EditarUsuarioViewModel
        {
            Id = usuario.id,
            Nombre = usuario.nombre,
            Apellido = usuario.apellido,
            Email = usuario.email,
            Rol = usuario.rol,
            ProfesionalId = usuario.profesional_id,
            Activo = usuario.activo,
            EsUsuarioLogueado = (usuario.id == usuarioActualId)
        };

        await CargarComboProfesionalesAsync(usuario.profesional_id);
        return View(model);
    }

    // POST: Usuarios/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditarUsuarioViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        var usuarioActualIdTexto = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int.TryParse(usuarioActualIdTexto, out int usuarioActualId);
        bool esUsuarioActual = (id == usuarioActualId);
        model.EsUsuarioLogueado = esUsuarioActual;

        // 1. Validar que el email no esté repetido por otro usuario
        if (!string.IsNullOrWhiteSpace(model.Email))
        {
            var emailNormalizado = model.Email.Trim().ToLower();
            var emailExiste = await _context.Usuarios
                .AnyAsync(u => u.id != id && u.email.ToLower() == emailNormalizado);

            if (emailExiste)
            {
                ModelState.AddModelError(nameof(model.Email), "Ya existe otro usuario registrado con este correo electrónico.");
            }
        }

        // 2. Si el rol es Profesional, validar médico vinculado
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

        // 3. Reglas de proteccion de autobloqueo para el Administrador actual
        if (esUsuarioActual)
        {
            if (model.Rol != Rol.Administrador)
            {
                ModelState.AddModelError(nameof(model.Rol), "No podés quitarte el rol de Administrador a vos mismo mientras estés en sesión.");
            }

            if (!model.Activo)
            {
                ModelState.AddModelError(nameof(model.Activo), "No podés desactivar tu propia cuenta mientras estés en sesión.");
            }
        }

        // 4. Validar contraseña nueva si fue ingresada
        if (!string.IsNullOrWhiteSpace(model.PasswordNueva))
        {
            if (model.PasswordNueva.Length < 6)
            {
                ModelState.AddModelError(nameof(model.PasswordNueva), "La nueva contraseña debe tener al menos 6 caracteres.");
            }

            if (model.PasswordNueva != model.ConfirmarPasswordNueva)
            {
                ModelState.AddModelError(nameof(model.ConfirmarPasswordNueva), "Las contraseñas no coinciden.");
            }
        }

        if (!ModelState.IsValid)
        {
            await CargarComboProfesionalesAsync(model.ProfesionalId);
            return View(model);
        }

        var existing = await _context.Usuarios.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.nombre = model.Nombre.Trim();
        existing.apellido = model.Apellido.Trim();
        existing.email = model.Email.Trim().ToLower();
        existing.rol = model.Rol;
        existing.profesional_id = model.Rol == Rol.Profesional ? model.ProfesionalId : null;
        existing.activo = model.Activo;

        // Si el administrador ingresó una nueva contraseña, hashearla y actualizarla
        if (!string.IsNullOrWhiteSpace(model.PasswordNueva))
        {
            existing.password_hash = _passwordHasher.HashPassword(existing, model.PasswordNueva);
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Datos del usuario {existing.email} actualizados con éxito.";
        return RedirectToAction(nameof(Index));
    }

    // POST: Usuarios/ToggleEstado/5 (Baja Lógica / Reactivación)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleEstado(int id)
    {
        var usuarioActualIdTexto = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int.TryParse(usuarioActualIdTexto, out int usuarioActualId);

        // Protección de seguridad: no autodesactivarse
        if (id == usuarioActualId)
        {
            TempData["ErrorMessage"] = "Acción denegada: no podés desactivar tu propia cuenta de Administrador mientras estés en sesión.";
            return RedirectToAction(nameof(Index));
        }

        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
        {
            return NotFound();
        }

        usuario.activo = !usuario.activo;
        await _context.SaveChangesAsync();

        var estadoTexto = usuario.activo ? "activado" : "desactivado";
        TempData["SuccessMessage"] = $"El usuario {usuario.email} fue {estadoTexto} correctamente.";

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

