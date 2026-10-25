using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using mvc.Models;

namespace mvc.Controllers;

[Authorize]
public class ProfesionalesController : Controller
{
    private readonly AppDbContext _context;

    public ProfesionalesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: Profesionales
    public async Task<IActionResult> Index(string? search, int? especialidadId, string? estadoFiltro)
    {
        var query = _context.Profesionales
            .Include(p => p.Especialidad)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(p =>
                p.nombre.ToLower().Contains(searchLower) ||
                p.apellido.ToLower().Contains(searchLower) ||
                p.dni.Contains(searchLower) ||
                p.matricula.ToLower().Contains(searchLower));
        }

        if (especialidadId.HasValue && especialidadId.Value > 0)
        {
            query = query.Where(p => p.especialidad_id == especialidadId.Value);
        }

        if (estadoFiltro == "activos")
        {
            query = query.Where(p => p.estado);
        }
        else if (estadoFiltro == "inactivos")
        {
            query = query.Where(p => !p.estado);
        }

        var profesionales = await query
            .OrderBy(p => p.apellido)
            .ThenBy(p => p.nombre)
            .ToListAsync();

        var especialidades = await _context.Especialidades
            .OrderBy(e => e.nombre)
            .ToListAsync();

        ViewBag.Especialidades = new SelectList(especialidades, "id", "nombre", especialidadId);
        ViewBag.Search = search;
        ViewBag.EspecialidadId = especialidadId;
        ViewBag.EstadoFiltro = estadoFiltro ?? "todos";

        return View(profesionales);
    }

    // GET: Profesionales/Create
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Create()
    {
        await CargarComboEspecialidadesAsync();
        return View(new Profesional { estado = true });
    }

    // POST: Profesionales/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Create(Profesional model)
    {
        // 1. Validar DNI único
        if (!string.IsNullOrWhiteSpace(model.dni))
        {
            var dniNormalizado = model.dni.Trim();
            var dniExiste = await _context.Profesionales.AnyAsync(p => p.dni == dniNormalizado);
            if (dniExiste)
            {
                ModelState.AddModelError("dni", "Ya existe un profesional registrado con este DNI.");
            }
        }

        // 2. Validar Matrícula única
        if (!string.IsNullOrWhiteSpace(model.matricula))
        {
            var matNormalizada = model.matricula.Trim().ToLower();
            var matExiste = await _context.Profesionales
                .AnyAsync(p => p.matricula.ToLower() == matNormalizada);
            if (matExiste)
            {
                ModelState.AddModelError("matricula", "Ya existe un profesional registrado con esta matrícula.");
            }
        }

        // 3. Validar especialidad existente
        var especialidadValida = await _context.Especialidades
            .AnyAsync(e => e.id == model.especialidad_id);
        if (!especialidadValida)
        {
            ModelState.AddModelError("especialidad_id", "Debe seleccionar una especialidad médica válida.");
        }

        if (!ModelState.IsValid)
        {
            await CargarComboEspecialidadesAsync(model.especialidad_id);
            return View(model);
        }

        // Normalización y guardado
        model.nombre = model.nombre.Trim();
        model.apellido = model.apellido.Trim();
        model.dni = model.dni.Trim();
        model.matricula = model.matricula.Trim();
        model.telefono = model.telefono?.Trim();
        model.email = model.email?.Trim();

        _context.Profesionales.Add(model);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"El/La profesional Dr./Dra. {model.apellido}, {model.nombre} fue registrado/a con éxito.";
        return RedirectToAction(nameof(Index));
    }

    private async Task CargarComboEspecialidadesAsync(int? seleccionado = null)
    {
        var especialidades = await _context.Especialidades
            .Where(e => e.estado || e.id == seleccionado)
            .OrderBy(e => e.nombre)
            .ToListAsync();

        ViewBag.Especialidades = new SelectList(especialidades, "id", "nombre", seleccionado);
    }
}
