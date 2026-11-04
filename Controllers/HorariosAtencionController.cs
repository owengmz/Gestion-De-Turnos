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

        public async Task<IActionResult> Index()
        {
            var horarios = await _context.HorariosAtencion
                .Include(h => h.Profesional)
                .OrderBy(h => h.profesional_id)
                .ThenBy(h => h.dia_semana)
                .ThenBy(h => h.hora_desde)
                .ToListAsync();

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