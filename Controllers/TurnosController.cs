
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
