
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mvc.Models;

namespace mvc.Controllers
{
    [Authorize]
    public class TurnosController : Controller
    {
        private readonly AppDbContext _context;

        public TurnosController(AppDbContext context)
        {
            _context = context;
        }
        
       
    public async Task<IActionResult> Index(int pagina = 1)
    {
        int registrosPorPagina = 10;

        var consulta = _context.Turnos
            .Include(t => t.Paciente)
            .Include(t => t.Profesional)
            .OrderBy(t => t.fecha)
            .ThenBy(t => t.hora);

        int totalRegistros = await consulta.CountAsync();

        int totalPaginas = (int)Math.Ceiling(
            (double)totalRegistros / registrosPorPagina
        );

        var turnos = await consulta
            .Skip((pagina - 1) * registrosPorPagina)
            .Take(registrosPorPagina)
            .ToListAsync();

        ViewBag.PaginaActual = pagina;
        ViewBag.TotalPaginas = totalPaginas;

        return View(turnos);
    }
   
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }
  
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Turno turno)
    {
        // Identificar al usuario que inicio sesion
        var usuarioIdTexto = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(usuarioIdTexto, out int usuarioId))
            return Forbid();

        // Validar paciente
        var paciente = await _context.Pacientes
            .FirstOrDefaultAsync(p => p.id == turno.paciente_id && p.estado);

        if (paciente == null)
            ModelState.AddModelError("paciente_id", "El paciente no existe o está inactivo.");

        // Validar profesional
        var profesional = await _context.Profesionales
            .FirstOrDefaultAsync(p => p.id == turno.profesional_id && p.estado);

        if (profesional == null)
            ModelState.AddModelError("profesional_id", "El profesional no existe o está inactivo.");

        // Validar fecha
        if (turno.fecha < DateOnly.FromDateTime(DateTime.Today))
            ModelState.AddModelError("fecha", "No se pueden registrar turnos en fechas pasadas.");

        // Validar horario de atención
        if (profesional != null)
        {
            string diaSemana = turno.fecha.DayOfWeek switch
            {
                DayOfWeek.Monday => "Lunes",
                DayOfWeek.Tuesday => "Martes",
                DayOfWeek.Wednesday => "Miercoles",
                DayOfWeek.Thursday => "Jueves",
                DayOfWeek.Friday => "Viernes",
                DayOfWeek.Saturday => "Sabado",
                DayOfWeek.Sunday => "Domingo",
                _ => ""
            };

            bool dentroHorario = await _context.HorariosAtencion.AnyAsync(h =>
                h.profesional_id == turno.profesional_id &&
                h.estado &&
                h.dia_semana == diaSemana &&
                turno.hora >= h.hora_desde &&
                turno.hora < h.hora_hasta);

            if (!dentroHorario)
                ModelState.AddModelError("hora", "El profesional no atiende en ese día u horario.");
        }

        // Comprobar disponibilidad del profesional
        bool profesionalOcupado = await _context.Turnos.AnyAsync(t =>
            t.profesional_id == turno.profesional_id &&
            t.fecha == turno.fecha &&
            t.hora == turno.hora &&
            t.estado != "Cancelado");

        if (profesionalOcupado)
            ModelState.AddModelError("hora", "El profesional ya tiene un turno en ese horario.");

        // Comprobar disponibilidad del paciente
        bool pacienteOcupado = await _context.Turnos.AnyAsync(t =>
            t.paciente_id == turno.paciente_id &&
            t.fecha == turno.fecha &&
            t.hora == turno.hora &&
            t.estado != "Cancelado");

        if (pacienteOcupado)
            ModelState.AddModelError("hora", "El paciente ya tiene un turno en ese horario.");

        // Si existen errores, volver al formulario
        if (!ModelState.IsValid)
            return View(turno);

        // Registrar auditoría
        turno.usuario_creador_id = usuarioId;
        turno.usuario_modificador_id = null;
        turno.fecha_creacion = DateTime.Now;
        turno.fecha_modificacion = null;
        turno.estado = "Pendiente";

        _context.Turnos.Add(turno);
        await _context.SaveChangesAsync();

        TempData["Mensaje"] = "Turno registrado correctamente.";

        return RedirectToAction(nameof(Index));
    }

   
    [HttpGet]
    public async Task<IActionResult> BuscarPacientes(string term)
    {
        var pacientes = await _context.Pacientes
            .AsNoTracking()
            .Where(p =>
                p.nombre.Contains(term) ||
                p.apellido.Contains(term) ||
                p.dni.Contains(term))
            .OrderBy(p => p.apellido)
            .Take(10)
            .Select(p => new
            {
                id = p.id,
                text = p.nombre + " " + p.apellido + " - DNI: " + p.dni
            })
            .ToListAsync();

        return Json(new { results = pacientes });
    }

    [HttpGet]
    public async Task<IActionResult> BuscarProfesionales(string term)
    {
        var profesionales = await _context.Profesionales
            .AsNoTracking()
            .Where(p =>
                p.estado &&
                (p.nombre.Contains(term) ||
                p.apellido.Contains(term) ||
                p.dni.Contains(term)))
            .OrderBy(p => p.apellido)
            .Take(10)
            .Select(p => new
            {
                id = p.id,
                text = p.nombre + " " + p.apellido + " - DNI: " + p.dni
            })
            .ToListAsync();

        return Json(new { results = profesionales });
    }


    }
}
