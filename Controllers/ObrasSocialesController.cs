using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mvc.Models;

namespace mvc.Controllers
{
  // solo usuarios con sesion iniciada pueden usar este controlador
  [Authorize]
  public class ObrasSocialesController : Controller
  {
    // acceso a la base de datos, lo entrega el framework
    private readonly AppDbContext _context;

    // inyeccion de dependencias por constructor
    public ObrasSocialesController(AppDbContext context)
    {
      _context = context;
    }

    // GET: /ObrasSociales/Listar?buscar=osde&page=1&pageSize=10
    [HttpGet]
    public async Task<IActionResult> Listar(string? buscar, int page = 1, int pageSize = 10)
    {
      // consulta armada pero todavia no ejecutada
      var consulta = _context.ObrasSociales.AsNoTracking().AsQueryable();

      // filtro de busqueda por nombre (opcional)
      if (!string.IsNullOrWhiteSpace(buscar))
      {
        var texto = buscar.Trim().ToLower();
        consulta = consulta.Where(o => o.nombre.ToLower().Contains(texto));
      }

      // el orden es obligatorio para que la paginacion sea estable
      consulta = consulta.OrderBy(o => o.nombre);

      // trae solo la pagina pedida y el total
      var resultado = await PagedResult<ObraSocial>.CreateAsync(consulta, page, pageSize);

      return Json(resultado);
    }

    // POST: CREATE  (recibe JSON) 
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] ObraSocial modelo)
    {
      // validacion por anotaciones (Required, StringLength)
      if (!ModelState.IsValid)
      {
        return ErrorValidacion();
      }

      var nombre = modelo.nombre.Trim();

      if (await NombreRepetido(nombre, 0))
      {
        return BadRequest(new { ok = false, mensaje = "Ya existe una obra social con ese nombre" });
      }

      // se copian solo los campos permitidos, no el objeto recibido completo
      var obra = new ObraSocial
      {
        nombre = nombre,
        descripcion = modelo.descripcion?.Trim(),
        estado = true
      };

      _context.ObrasSociales.Add(obra);
      await _context.SaveChangesAsync();

      return Json(new { ok = true, id = obra.id, mensaje = "Obra social creada" });
    }

    // POST: EDIT (recibe JSON con el id)
    [HttpPost]
    public async Task<IActionResult> Editar([FromBody] ObraSocial modelo)
    {
      if (!ModelState.IsValid)
      {
        return ErrorValidacion();
      }

      var obra = await _context.ObrasSociales.FindAsync(modelo.id);

      if (obra == null)
      {
        return NotFound(new { ok = false, mensaje = "La obra social no existe" });
      }

      var nombre = modelo.nombre.Trim();

      if (await NombreRepetido(nombre, obra.id))
      {
        return BadRequest(new { ok = false, mensaje = "Ya existe una obra social con ese nombre" });
      }

      obra.nombre = nombre;
      obra.descripcion = modelo.descripcion?.Trim();

      await _context.SaveChangesAsync();

      return Json(new { ok = true, mensaje = "Obra social actualizada" });
    }

    // POST: /ObrasSociales/CambiarEstado/5  (baja logica)
    [HttpPost]
    public async Task<IActionResult> CambiarEstado(int id)
    {
      var obra = await _context.ObrasSociales.FindAsync(id);

      if (obra == null)
      {
        return NotFound(new { ok = false, mensaje = "La obra social no existe" });
      }

      obra.estado = !obra.estado;
      await _context.SaveChangesAsync();

      return Json(new
      {
        ok = true,
        estado = obra.estado,
        mensaje = obra.estado ? "Obra social activada" : "Obra social desactivada"
      });
    }

    // POST: ELIMINAR (baja fisica, solo administrador)
    [HttpPost]
    [Authorize(Roles = nameof(Rol.Administrador))]
    public async Task<IActionResult> Eliminar(int id)
    {
      var obra = await _context.ObrasSociales.FindAsync(id);

      if (obra == null)
      {
        return NotFound(new { ok = false, mensaje = "La obra social no existe" });
      }

      // la relacion es Restrict: con pacientes asociados no se puede borrar
      bool tienePacientes = await _context.Pacientes.AnyAsync(p => p.obra_social_id == id);

      if (tienePacientes)
      {
        return BadRequest(new
        {
          ok = false,
          mensaje = "No se puede eliminar: tiene pacientes asociados. Podes desactivarla."
        });
      }

      _context.ObrasSociales.Remove(obra);
      await _context.SaveChangesAsync();

      return Json(new { ok = true, mensaje = "Obra social eliminada" });
    }
    // metodos para validar y armar respuestas de error
    // indica si ya existe otra obra social con ese nombre
    private Task<bool> NombreRepetido(string nombre, int idActual)
    {
      var texto = nombre.ToLower();
      return _context.ObrasSociales
        .AnyAsync(o => o.id != idActual && o.nombre.ToLower() == texto);
    }

    // arma la respuesta 400 con los mensajes de las anotaciones
    private IActionResult ErrorValidacion()
    {
      var errores = ModelState.Values
        .SelectMany(v => v.Errors)
        .Select(e => e.ErrorMessage)
        .ToList();

      return BadRequest(new { ok = false, mensaje = string.Join(" ", errores), errores });
    }
  }
}