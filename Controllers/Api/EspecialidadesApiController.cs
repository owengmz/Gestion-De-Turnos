using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mvc.Models;

namespace mvc.Controllers.Api;

[ApiController]
[Route("api/especialidades")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class EspecialidadesApiController : ControllerBase
{
    private readonly AppDbContext _context;

    public EspecialidadesApiController(AppDbContext context)
    {
        _context = context;
    }

    // GET: /api/especialidades
    [HttpGet]
    public async Task<IActionResult> GetEspecialidades()
    {
        var especialidades = await _context.Especialidades
            .AsNoTracking()
            .Where(e => e.estado) // Solo especialidades activas
            .OrderBy(e => e.nombre)
            .Select(e => new
            {
                id = e.id,
                nombre = e.nombre,
                descripcion = e.descripcion,
                estado = e.estado,
                cantidadMedicos = e.Profesionales.Count(p => p.estado)
            })
            .ToListAsync();

        return Ok(new
        {
            total = especialidades.Count,
            datos = especialidades
        });
    }

    // GET: /api/especialidades/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetEspecialidadPorId(int id)
    {
        var especialidad = await _context.Especialidades
            .AsNoTracking()
            .Include(e => e.Profesionales)
            .FirstOrDefaultAsync(e => e.id == id);

        if (especialidad == null)
        {
            return NotFound(new { mensaje = $"No se encontró ninguna especialidad con el ID {id}." });
        }

        var resultado = new
        {
            id = especialidad.id,
            nombre = especialidad.nombre,
            descripcion = especialidad.descripcion,
            estado = especialidad.estado,
            medicos = especialidad.Profesionales
                .Where(p => p.estado)
                .OrderBy(p => p.apellido)
                .Select(p => new
                {
                    id = p.id,
                    nombreCompleto = $"Dr./Dra. {p.apellido}, {p.nombre}",
                    matricula = p.matricula,
                    email = p.email
                })
        };

        return Ok(resultado);
    }
}
