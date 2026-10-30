using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mvc.Models;

namespace mvc.Controllers
{
  // solo usuarios con sesion iniciada pueden usar este controlador
  [Authorize]
  public class PacientesController : Controller
  {
    // acceso a la base de datos, lo entrega el framework
    private readonly AppDbContext _context;

    // inyeccion de dependencias por constructor
    public PacientesController(AppDbContext context)
    {
      _context = context;
    }

    // GET: /Pacientes?buscar=perez&estadoFiltro=activos&pagina=1
    public async Task<IActionResult> Index(string? buscar, string? estadoFiltro, int pagina = 1)
    {
      // consulta armada pero todavia no ejecutada
      var consulta = _context.Pacientes
        .AsNoTracking()
        .Include(p => p.ObraSocial)
        .AsQueryable();

      // busqueda por dni, o por nombre y apellido en cualquier orden
      if (!string.IsNullOrWhiteSpace(buscar))
      {
        var texto = buscar.Trim().ToLower();
        consulta = consulta.Where(p =>
          p.dni.Contains(texto) ||
          (p.apellido + " " + p.nombre).ToLower().Contains(texto) ||
          (p.nombre + " " + p.apellido).ToLower().Contains(texto));
      }

      // filtro por estado
      if (estadoFiltro == "activos")
      {
        consulta = consulta.Where(p => p.estado);
      }
      else if (estadoFiltro == "inactivos")
      {
        consulta = consulta.Where(p => !p.estado);
      }

      // el orden es obligatorio para que la paginacion sea estable
      consulta = consulta.OrderBy(p => p.apellido).ThenBy(p => p.nombre);

      // trae solo la pagina pedida y el total
      var resultado = await ResultadoPaginado<Paciente>.CrearAsync(consulta, pagina, 10);

      // datos para mantener los filtros en la vista
      ViewBag.Buscar = buscar;
      ViewBag.EstadoFiltro = estadoFiltro ?? "todos";

      return View(resultado);
    }
  }
}