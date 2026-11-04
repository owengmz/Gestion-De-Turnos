using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mvc.Models;

namespace mvc.Controllers.Api;

[ApiController]
[Route("api/profesionales")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ProfesionalesApiController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProfesionalesApiController(AppDbContext context)
    {
        _context = context;
    }

    // GET: /api/profesionales
    // Filtros opcionales: search (texto), especialidadId (filtro por especialidad)
    [HttpGet]
    public async Task<IActionResult> GetProfesionales([FromQuery] string? search, [FromQuery] int? especialidadId)
    {
        var query = _context.Profesionales
            .AsNoTracking()
            .Include(p => p.Especialidad)
            .Where(p => p.estado); // Solo profesionales habilitados

        if (especialidadId.HasValue && especialidadId.Value > 0)
        {
            query = query.Where(p => p.especialidad_id == especialidadId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(p =>
                p.nombre.ToLower().Contains(searchLower) ||
                p.apellido.ToLower().Contains(searchLower) ||
                p.dni.Contains(searchLower) ||
                p.matricula.ToLower().Contains(searchLower) ||
                (p.Especialidad != null && p.Especialidad.nombre.ToLower().Contains(searchLower)));
        }

        var profesionales = await query
            .OrderBy(p => p.apellido)
            .ThenBy(p => p.nombre)
            .Select(p => new
            {
                id = p.id,
                nombre = p.nombre,
                apellido = p.apellido,
                nombreCompleto = $"Dr./Dra. {p.apellido}, {p.nombre}",
                dni = p.dni,
                matricula = p.matricula,
                email = p.email,
                telefono = p.telefono,
                estado = p.estado,
                especialidad = new
                {
                    id = p.Especialidad != null ? p.Especialidad.id : 0,
                    nombre = p.Especialidad != null ? p.Especialidad.nombre : "Sin especialidad"
                }
            })
            .ToListAsync();

        return Ok(new
        {
            total = profesionales.Count,
            datos = profesionales
        });
    }

    // GET: /api/profesionales/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProfesionalPorId(int id)
    {
        var profesional = await _context.Profesionales
            .AsNoTracking()
            .Include(p => p.Especialidad)
            .Include(p => p.HorariosAtencion)
            .FirstOrDefaultAsync(p => p.id == id);

        if (profesional == null)
        {
            return NotFound(new { mensaje = $"No se encontró ningún profesional con el ID {id}." });
        }

        var resultado = new
        {
            id = profesional.id,
            nombre = profesional.nombre,
            apellido = profesional.apellido,
            nombreCompleto = $"Dr./Dra. {profesional.apellido}, {profesional.nombre}",
            dni = profesional.dni,
            matricula = profesional.matricula,
            email = profesional.email,
            telefono = profesional.telefono,
            estado = profesional.estado,
            especialidad = new
            {
                id = profesional.Especialidad?.id ?? 0,
                nombre = profesional.Especialidad?.nombre ?? "Sin especialidad",
                descripcion = profesional.Especialidad?.descripcion
            },
            horariosAtencion = profesional.HorariosAtencion
                .Where(h => h.estado)
                .OrderBy(h => h.dia_semana)
                .Select(h => new
                {
                    id = h.id,
                    diaSemana = h.dia_semana,
                    horaDesde = h.hora_desde.ToString(@"hh\:mm"),
                    horaHasta = h.hora_hasta.ToString(@"hh\:mm")
                })
        };

        return Ok(resultado);
    }
}

