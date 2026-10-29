using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mvc.Models;

namespace mvc.Controllers;

[Authorize]
public class EspecialidadesController : Controller
{
    private readonly AppDbContext _context;

    public EspecialidadesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: Especialidades
    public async Task<IActionResult> Index(string? search, string? estadoFiltro)
    {
        var query = _context.Especialidades
            .Include(e => e.Profesionales)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(e =>
                e.nombre.ToLower().Contains(searchLower) ||
                (e.descripcion != null && e.descripcion.ToLower().Contains(searchLower)));
        }

        if (estadoFiltro == "activos")
        {
            query = query.Where(e => e.estado);
        }
        else if (estadoFiltro == "inactivos")
        {
            query = query.Where(e => !e.estado);
        }

        var especialidades = await query
            .OrderBy(e => e.nombre)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.EstadoFiltro = estadoFiltro ?? "todos";

        return View(especialidades);
    }

    // GET: Especialidades/Create
    [Authorize(Roles = "Administrador")]
    public IActionResult Create()
    {
        return View(new Especialidad { estado = true });
    }

    // POST: Especialidades/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Create(Especialidad model)
    {
        if (!string.IsNullOrWhiteSpace(model.nombre))
        {
            var nombreNormalizado = model.nombre.Trim().ToLower();
            var yaExiste = await _context.Especialidades
                .AnyAsync(e => e.nombre.ToLower() == nombreNormalizado);

            if (yaExiste)
            {
                ModelState.AddModelError("nombre", "Ya existe una especialidad con este nombre.");
            }
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        model.nombre = model.nombre.Trim();
        model.descripcion = model.descripcion?.Trim();

        _context.Especialidades.Add(model);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"La especialidad '{model.nombre}' fue creada exitosamente.";
        return RedirectToAction(nameof(Index));
    }

    // GET: Especialidades/Edit/5
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var especialidad = await _context.Especialidades.FindAsync(id);
        if (especialidad == null)
        {
            return NotFound();
        }

        return View(especialidad);
    }

    // POST: Especialidades/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Edit(int id, Especialidad model)
    {
        if (id != model.id)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(model.nombre))
        {
            var nombreNormalizado = model.nombre.Trim().ToLower();
            var yaExiste = await _context.Especialidades
                .AnyAsync(e => e.id != id && e.nombre.ToLower() == nombreNormalizado);

            if (yaExiste)
            {
                ModelState.AddModelError("nombre", "Ya existe otra especialidad con este nombre.");
            }
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existing = await _context.Especialidades.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.nombre = model.nombre.Trim();
        existing.descripcion = model.descripcion?.Trim();
        existing.estado = model.estado;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"La especialidad '{existing.nombre}' fue actualizada exitosamente.";
        return RedirectToAction(nameof(Index));
    }

    // POST: Especialidades/ToggleEstado/5 (Baja Lógica / Reactivación)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ToggleEstado(int id)
    {
        var especialidad = await _context.Especialidades
            .Include(e => e.Profesionales)
            .FirstOrDefaultAsync(e => e.id == id);

        if (especialidad == null)
        {
            return NotFound();
        }

        especialidad.estado = !especialidad.estado;
        await _context.SaveChangesAsync();

        var estadoTexto = especialidad.estado ? "activada" : "desactivada";
        TempData["SuccessMessage"] = $"La especialidad '{especialidad.nombre}' fue {estadoTexto} correctamente.";

        return RedirectToAction(nameof(Index));
    }
}
