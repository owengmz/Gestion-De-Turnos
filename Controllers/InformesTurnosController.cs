using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mvc.Models;

namespace mvc.Controllers
{
    [Authorize]
    public class InformesTurnosController : Controller
    {
        private readonly AppDbContext _context;

        public InformesTurnosController(AppDbContext context)
        {
            _context = context;
        }     

        // GET: /InformesTurnos
        public async Task<IActionResult> Index(
            DateOnly? fechaDesde,
            DateOnly? fechaHasta,
            int? profesionalId,
            string? estado,
            int pagina = 1)
        {
            int registrosPorPagina = 10;

            var consulta = _context.Turnos
                .Include(t => t.Paciente)
                .Include(t => t.Profesional)
                .AsQueryable();

            if (fechaDesde.HasValue)
                consulta = consulta.Where(t => t.fecha >= fechaDesde.Value);

            if (fechaHasta.HasValue)
                consulta = consulta.Where(t => t.fecha <= fechaHasta.Value);

            if (profesionalId.HasValue)
                consulta = consulta.Where(t => t.profesional_id == profesionalId.Value);

            if (!string.IsNullOrWhiteSpace(estado))
                consulta = consulta.Where(t => t.estado == estado);

            ViewBag.TotalPendientes = await consulta
                .CountAsync(t => t.estado == "Pendiente");

            ViewBag.TotalConfirmados = await consulta
                .CountAsync(t => t.estado == "Confirmado");

            ViewBag.TotalAtendidos = await consulta
                .CountAsync(t => t.estado == "Atendido");

            ViewBag.TotalCancelados = await consulta
                .CountAsync(t => t.estado == "Cancelado");

            ViewBag.TotalAusentes = await consulta
                .CountAsync(t => t.estado == "Ausente"); 

            ViewBag.DatosGrafico = new[]
            {
                new { Estado = "Pendiente", Cantidad = ViewBag.TotalPendientes },
                new { Estado = "Confirmado", Cantidad = ViewBag.TotalConfirmados },
                new { Estado = "Atendido", Cantidad = ViewBag.TotalAtendidos },
                new { Estado = "Cancelado", Cantidad = ViewBag.TotalCancelados },
                new { Estado = "Ausente", Cantidad = ViewBag.TotalAusentes }
            };       

            consulta = consulta
                .OrderByDescending(t => t.fecha)
                .ThenByDescending(t => t.hora);

            int totalRegistros = await consulta.CountAsync();

            int totalPaginas = (int)Math.Ceiling(
                (double)totalRegistros / registrosPorPagina);

            pagina = Math.Max(1, Math.Min(pagina, Math.Max(1, totalPaginas)));

            var turnos = await consulta
                .Skip((pagina - 1) * registrosPorPagina)
                .Take(registrosPorPagina)
                .ToListAsync();

            ViewBag.FechaDesde = fechaDesde?.ToString("yyyy-MM-dd");
            ViewBag.FechaHasta = fechaHasta?.ToString("yyyy-MM-dd");
            ViewBag.ProfesionalId = profesionalId;
            ViewBag.Estado = estado;
            ViewBag.PaginaActual = pagina;
            ViewBag.TotalPaginas = totalPaginas;

            ViewBag.Profesionales = await _context.Profesionales
                .Where(p => p.estado)
                .OrderBy(p => p.apellido)
                .ToListAsync();

            ViewBag.TotalRegistros = totalRegistros;

            return View(turnos);
        }
    }
}