using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mvc.Models;

namespace mvc.Controllers
{
    [Authorize]
    public class HorariosAtencionController : Controller
    {
        private readonly AppDbContext _context;

        public HorariosAtencionController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int pagina = 1)
        {
            int registrosPorPagina = 10;

            var consulta = _context.HorariosAtencion
                .Include(h => h.Profesional)
                .OrderBy(h => h.profesional_id)
                .ThenBy(h => h.dia_semana)
                .ThenBy(h => h.hora_desde);

            int totalRegistros = await consulta.CountAsync();

            int totalPaginas = (int)Math.Ceiling(
                (double)totalRegistros / registrosPorPagina
            );

            pagina = Math.Max(1, Math.Min(pagina, Math.Max(1, totalPaginas)));

            var horarios = await consulta
                .Skip((pagina - 1) * registrosPorPagina)
                .Take(registrosPorPagina)
                .ToListAsync();

            ViewBag.PaginaActual = pagina;
            ViewBag.TotalPaginas = totalPaginas;

            return View(horarios);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await CargarProfesionales();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HorarioAtencion horario)
        {
            if (horario.hora_hasta <= horario.hora_desde)
            {
                ModelState.AddModelError(
                    "hora_hasta",
                    "La hora de finalización debe ser posterior a la hora de inicio."
                );
            }

            var profesionalExiste = await _context.Profesionales
                .AnyAsync(p => p.id == horario.profesional_id && p.estado);

            if (!profesionalExiste)
            {
                ModelState.AddModelError(
                    "profesional_id",
                    "Debe seleccionar un profesional activo."
                );
            }

            var horarioDuplicado = await _context.HorariosAtencion
                .AnyAsync(h =>
                    h.profesional_id == horario.profesional_id &&
                    h.dia_semana == horario.dia_semana &&
                    h.estado &&
                    horario.hora_desde < h.hora_hasta &&
                    horario.hora_hasta > h.hora_desde
                );

            if (horarioDuplicado)
            {
                ModelState.AddModelError(
                    "",
                    "El profesional ya tiene un horario registrado que se superpone con ese horario."
                );
            }

            if (!ModelState.IsValid)
            {
                await CargarProfesionales();
                return View(horario);
            }

            horario.estado = true;

            _context.HorariosAtencion.Add(horario);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Horario creado correctamente.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var horario = await _context.HorariosAtencion
                .Include(h => h.Profesional)
                .FirstOrDefaultAsync(h => h.id == id);

            if (horario == null)
                return NotFound();

            await CargarProfesionales();

            return View(horario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, HorarioAtencion modelo)
        {
            if (id != modelo.id)
                return NotFound();

            var horario = await _context.HorariosAtencion
                .FirstOrDefaultAsync(h => h.id == id);

            if (horario == null)
                return NotFound();

            if (modelo.hora_hasta <= modelo.hora_desde)
            {
                ModelState.AddModelError(
                    "hora_hasta",
                    "La hora de finalización debe ser posterior a la hora de inicio."
                );
            }

            var profesionalExiste = await _context.Profesionales
                .AnyAsync(p => p.id == modelo.profesional_id && p.estado);

            if (!profesionalExiste)
            {
                ModelState.AddModelError(
                    "profesional_id",
                    "Debe seleccionar un profesional activo."
                );
            }

            var horarioDuplicado = await _context.HorariosAtencion
                .AnyAsync(h =>
                    h.id != id &&
                    h.profesional_id == modelo.profesional_id &&
                    h.dia_semana == modelo.dia_semana &&
                    h.estado &&
                    horario.estado &&
                    modelo.hora_desde < h.hora_hasta &&
                    modelo.hora_hasta > h.hora_desde
                );

            if (horarioDuplicado)
            {
                ModelState.AddModelError(
                    "",
                    "El profesional ya tiene otro horario que se superpone con ese horario."
                );
            }

            if (!ModelState.IsValid)
            {
                modelo.Profesional = await _context.Profesionales
                    .FirstOrDefaultAsync(p => p.id == modelo.profesional_id);

                await CargarProfesionales();

                return View(modelo);
            }

            horario.profesional_id = modelo.profesional_id;
            horario.dia_semana = modelo.dia_semana;
            horario.hora_desde = modelo.hora_desde;
            horario.hora_hasta = modelo.hora_hasta;

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Horario actualizado correctamente.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var horario = await _context.HorariosAtencion
                .FirstOrDefaultAsync(h => h.id == id);

            if (horario == null)
                return NotFound();

            horario.estado = !horario.estado;

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = horario.estado
                ? "Horario activado correctamente."
                : "Horario desactivado correctamente.";

            return RedirectToAction(nameof(Index));
        }

        private async Task CargarProfesionales()
        {
            ViewBag.Profesionales = await _context.Profesionales
                .Where(p => p.estado)
                .OrderBy(p => p.apellido)
                .Select(p => new
                {
                    p.id,
                    NombreCompleto = p.nombre + " " + p.apellido
                })
                .ToListAsync();
        }
    }
}