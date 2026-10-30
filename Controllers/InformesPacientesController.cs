using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mvc.Models;

namespace mvc.Controllers
{
  // los informes los usan administrador y recepcionista
  [Authorize(Roles = "Administrador,Recepcionista")]
  public class InformesPacientesController : Controller
  {
    private const int RegistrosPorPagina = 10;

    // acceso a la base de datos, lo entrega el framework
    private readonly AppDbContext _context;

    // inyeccion de dependencias por constructor
    public InformesPacientesController(AppDbContext context)
    {
      _context = context;
    }

    // POR OBRA SOCIAL
    // GET: /InformesPacientes/PorObraSocial?estadoFiltro=activos&pagina=1
    public async Task<IActionResult> PorObraSocial(string? estadoFiltro, int pagina = 1)
    {
      var pacientes = _context.Pacientes.AsNoTracking().AsQueryable();

      // por defecto se cuentan solo los pacientes activos
      estadoFiltro ??= "activos";
      if (estadoFiltro == "activos")
      {
        pacientes = pacientes.Where(p => p.estado);
      }
      else if (estadoFiltro == "inactivos")
      {
        pacientes = pacientes.Where(p => !p.estado);
      }

      // los pacientes sin obra social quedan en un grupo propio
      var consulta = pacientes
        .GroupBy(p => p.ObraSocial == null ? "Sin obra social" : p.ObraSocial.nombre)
        .Select(g => new FilaPorObraSocial
        {
          obra_social = g.Key,
          cantidad_pacientes = g.Count()
        })
        .OrderByDescending(f => f.cantidad_pacientes)
        .ThenBy(f => f.obra_social);

      var resultado = await ResultadoPaginado<FilaPorObraSocial>.CrearAsync(consulta, pagina, RegistrosPorPagina);

      ViewBag.EstadoFiltro = estadoFiltro;
      return View(resultado);
    }

    // SIN TURNOS
    // GET: /InformesPacientes/SinTurnos?buscar=perez&pagina=1
    public async Task<IActionResult> SinTurnos(string? buscar, int pagina = 1)
    {
      // pacientes activos que nunca tuvieron un turno
      var consulta = _context.Pacientes
        .AsNoTracking()
        .Include(p => p.ObraSocial)
        .Where(p => p.estado && !p.Turnos.Any());

      if (!string.IsNullOrWhiteSpace(buscar))
      {
        var texto = buscar.Trim().ToLower();
        consulta = consulta.Where(p =>
          p.dni.Contains(texto) ||
          (p.apellido + " " + p.nombre).ToLower().Contains(texto) ||
          (p.nombre + " " + p.apellido).ToLower().Contains(texto));
      }

      consulta = consulta.OrderBy(p => p.apellido).ThenBy(p => p.nombre);

      var resultado = await ResultadoPaginado<Paciente>.CrearAsync(consulta, pagina, RegistrosPorPagina);

      ViewBag.Buscar = buscar;
      return View(resultado);
    }

    // MAS TURNOS
    // GET: /InformesPacientes/MasTurnos?desde=2026-01-01&hasta=2026-12-31&estadoTurno=Atendido&pagina=1
    public async Task<IActionResult> MasTurnos(
      DateOnly? desde,
      DateOnly? hasta,
      string? estadoTurno,
      int pagina = 1)
    {
      var turnos = _context.Turnos.AsNoTracking().AsQueryable();

      if (desde.HasValue)
      {
        turnos = turnos.Where(t => t.fecha >= desde.Value);
      }

      if (hasta.HasValue)
      {
        turnos = turnos.Where(t => t.fecha <= hasta.Value);
      }

      if (!string.IsNullOrWhiteSpace(estadoTurno))
      {
        turnos = turnos.Where(t => t.estado == estadoTurno);
      }

      // un grupo por paciente, ordenado por cantidad de turnos
      var consulta = turnos
        .GroupBy(t => new { t.paciente_id, t.Paciente!.apellido, t.Paciente.nombre, t.Paciente.dni })
        .Select(g => new FilaMasTurnos
        {
          paciente_id = g.Key.paciente_id,
          apellido = g.Key.apellido,
          nombre = g.Key.nombre,
          dni = g.Key.dni,
          cantidad_turnos = g.Count(),
          ultimo_turno = g.Max(t => t.fecha)
        })
        .OrderByDescending(f => f.cantidad_turnos)
        .ThenBy(f => f.apellido)
        .ThenBy(f => f.nombre);

      var resultado = await ResultadoPaginado<FilaMasTurnos>.CrearAsync(consulta, pagina, RegistrosPorPagina);

      // estados existentes para armar el filtro sin escribirlos a mano
      ViewBag.Estados = await _context.Turnos
        .AsNoTracking()
        .Select(t => t.estado)
        .Distinct()
        .OrderBy(e => e)
        .ToListAsync();

      ViewBag.Desde = desde?.ToString("yyyy-MM-dd");
      ViewBag.Hasta = hasta?.ToString("yyyy-MM-dd");
      ViewBag.EstadoTurno = estadoTurno;
      return View(resultado);
    }
  }
}