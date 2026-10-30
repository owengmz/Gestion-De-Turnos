using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mvc.Models;
using mvc.Services;

namespace mvc.Controllers
{
  // solo usuarios con sesion iniciada pueden usar este controlador
  [Authorize]
  public class PacientesController : Controller
  {
    // campos que acepta el formulario (credencial_path nunca viene del cliente)
    private const string CamposCrear =
      "dni,nombre,apellido,fecha_nacimiento,telefono,email,direccion,numero_afiliado,obra_social_id,estado";
    private const string CamposEditar = "id," + CamposCrear;

    // acceso a la base de datos, lo entrega el framework
    private readonly AppDbContext _context;
    private readonly CredencialStorageService _credenciales;

    // inyeccion de dependencias por constructor
    public PacientesController(AppDbContext context, CredencialStorageService credenciales)
    {
      _context = context;
      _credenciales = credenciales;
    }

    // LISTAR
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

    // CREAR
    // GET: /Pacientes/Crear
    [Authorize(Roles = "Administrador,Recepcionista")]
    public IActionResult Crear()
    {
      // un paciente nuevo ya viene activo por defecto
      return View(new Paciente());
    }

    // POST: /Pacientes/Crear
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Recepcionista")]
    public async Task<IActionResult> Crear([Bind(CamposCrear)] Paciente paciente, IFormFile? credencial)
    {
      paciente.dni = paciente.dni.Trim();

      // el dni no puede repetirse
      if (await _context.Pacientes.AnyAsync(p => p.dni == paciente.dni))
      {
        ModelState.AddModelError(nameof(paciente.dni), "Ya existe un paciente con ese DNI");
      }

      await ValidarObraSocialAsync(paciente.obra_social_id);

      // la credencial es opcional, solo se valida si hay archivo
      var hayCredencial = credencial is not null && credencial.Length > 0;
      if (hayCredencial)
      {
        var error = await _credenciales.ValidarAsync(credencial);
        if (error is not null)
        {
          ModelState.AddModelError("credencial", error);
        }
      }

      if (!ModelState.IsValid)
      {
        await CargarObraSocialAsync(paciente.obra_social_id);
        return View(paciente);
      }

      string? ruta = null;
      try
      {
        if (hayCredencial)
        {
          ruta = await _credenciales.GuardarAsync(credencial!);
          paciente.credencial_path = ruta;
        }

        _context.Pacientes.Add(paciente);
        await _context.SaveChangesAsync();
      }
      catch (DbUpdateException)
      {
        // si falla la base no dejamos la imagen suelta
        _credenciales.EliminarSiEsGestionada(ruta);
        ModelState.AddModelError(nameof(paciente.dni), "No se pudo guardar. Verifica que el DNI no este repetido");
        await CargarObraSocialAsync(paciente.obra_social_id);
        return View(paciente);
      }

      TempData["Mensaje"] = "Paciente registrado correctamente";
      return RedirectToAction(nameof(Index));
    }

    // EDITAR
    // GET: /Pacientes/Editar/5
    [Authorize(Roles = "Administrador,Recepcionista")]
    public async Task<IActionResult> Editar(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var paciente = await _context.Pacientes.AsNoTracking().FirstOrDefaultAsync(p => p.id == id);
      if (paciente == null)
      {
        return NotFound();
      }

      await CargarObraSocialAsync(paciente.obra_social_id);
      return View(paciente);
    }

    // POST: /Pacientes/Editar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Recepcionista")]
    public async Task<IActionResult> Editar(
      int id,
      [Bind(CamposEditar)] Paciente paciente,
      IFormFile? credencial,
      bool quitarCredencial)
    {
      if (id != paciente.id)
      {
        return NotFound();
      }

      // traemos el original y copiamos solo los campos del form
      var actual = await _context.Pacientes.FindAsync(id);
      if (actual == null)
      {
        return NotFound();
      }

      var rutaAnterior = actual.credencial_path;
      paciente.dni = paciente.dni.Trim();

      // el dni no puede repetirse en otro paciente
      if (await _context.Pacientes.AnyAsync(p => p.dni == paciente.dni && p.id != id))
      {
        ModelState.AddModelError(nameof(paciente.dni), "Ya existe otro paciente con ese DNI");
      }

      await ValidarObraSocialAsync(paciente.obra_social_id);

      var hayCredencial = credencial is not null && credencial.Length > 0;
      if (hayCredencial)
      {
        var error = await _credenciales.ValidarAsync(credencial);
        if (error is not null)
        {
          ModelState.AddModelError("credencial", error);
        }
      }

      if (!ModelState.IsValid)
      {
        // devolvemos para que la vista la muestre
        paciente.credencial_path = rutaAnterior;
        await CargarObraSocialAsync(paciente.obra_social_id);
        return View(paciente);
      }

      string? rutaNueva = null;
      try
      {
        if (hayCredencial)
        {
          rutaNueva = await _credenciales.GuardarAsync(credencial!);
          actual.credencial_path = rutaNueva;
        }
        else if (quitarCredencial)
        {
          actual.credencial_path = null;
        }

        actual.dni = paciente.dni;
        actual.nombre = paciente.nombre;
        actual.apellido = paciente.apellido;
        actual.fecha_nacimiento = paciente.fecha_nacimiento;
        actual.telefono = paciente.telefono;
        actual.email = paciente.email;
        actual.direccion = paciente.direccion;
        actual.numero_afiliado = paciente.numero_afiliado;
        actual.obra_social_id = paciente.obra_social_id;
        actual.estado = paciente.estado;

        await _context.SaveChangesAsync();
      }
      catch (DbUpdateException)
      {
        // si falla la base no dejamos la imagen nueva suelta
        _credenciales.EliminarSiEsGestionada(rutaNueva);
        ModelState.AddModelError(nameof(paciente.dni), "No se pudo guardar. Verifica que el DNI no este repetido");
        paciente.credencial_path = rutaAnterior;
        await CargarObraSocialAsync(paciente.obra_social_id);
        return View(paciente);
      }

      // la base ya guardo: recien ahora se borra la imagen anterior
      if (hayCredencial || quitarCredencial)
      {
        _credenciales.EliminarSiEsGestionada(rutaAnterior);
      }

      TempData["Mensaje"] = "Paciente actualizado correctamente";
      return RedirectToAction(nameof(Index));
    }

    // ESTADO
    // POST: /Pacientes/CambiarEstado/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Recepcionista")]
    public async Task<IActionResult> CambiarEstado(int id)
    {
      var paciente = await _context.Pacientes.FindAsync(id);
      if (paciente == null)
      {
        return NotFound();
      }

      paciente.estado = !paciente.estado;
      await _context.SaveChangesAsync();

      TempData["Mensaje"] = paciente.estado
        ? "Paciente activado correctamente"
        : "Paciente desactivado correctamente";
      return RedirectToAction(nameof(Index));
    }

    // ELIMINAR
    // POST: /Pacientes/Eliminar/5
    [Authorize(Roles = "Administrador")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
      var paciente = await _context.Pacientes.FindAsync(id);
      if (paciente == null)
      {
        return NotFound();
      }

      // un paciente con turnos no se borra, se desactiva
      if (await _context.Turnos.AnyAsync(t => t.paciente_id == id))
      {
        TempData["Error"] = "No se puede eliminar el paciente porque tiene turnos asociados. Desactivalo en su lugar.";
        return RedirectToAction(nameof(Index));
      }

      var ruta = paciente.credencial_path;

      try
      {
        _context.Pacientes.Remove(paciente);
        await _context.SaveChangesAsync();
      }
      catch (DbUpdateException)
      {
        TempData["Error"] = "No se pudo eliminar el paciente.";
        return RedirectToAction(nameof(Index));
      }

      // la base ya borro el registro: ahora se borra la imagen
      _credenciales.EliminarSiEsGestionada(ruta);

      TempData["Mensaje"] = "Paciente eliminado correctamente";
      return RedirectToAction(nameof(Index));
    }

    // DETALLES
    // GET: /Pacientes/Detalles/5
    public async Task<IActionResult> Detalles(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var paciente = await _context.Pacientes
        .AsNoTracking()
        .Include(p => p.ObraSocial)
        .Include(p => p.Turnos)
        .FirstOrDefaultAsync(p => p.id == id);

      if (paciente == null)
      {
        return NotFound();
      }

      return View(paciente);
    }

    // BUSCAR
    // GET: /Pacientes/Buscar?term=perez&page=1  (formato Select2)
    [HttpGet]
    public async Task<IActionResult> Buscar(string? term, int page = 1)
    {
      const int porPagina = 10;
      if (page < 1)
      {
        page = 1;
      }

      // solo pacientes activos, se pueden asignar a turnos
      var consulta = _context.Pacientes
        .AsNoTracking()
        .Where(p => p.estado);

      if (!string.IsNullOrWhiteSpace(term))
      {
        var texto = term.Trim().ToLower();
        consulta = consulta.Where(p =>
          p.dni.Contains(texto) ||
          (p.apellido + " " + p.nombre).ToLower().Contains(texto) ||
          (p.nombre + " " + p.apellido).ToLower().Contains(texto));
      }

      // se piden 11 para saber si hay otra pagina sin contar todo
      var encontrados = await consulta
        .OrderBy(p => p.apellido).ThenBy(p => p.nombre)
        .Skip((page - 1) * porPagina)
        .Take(porPagina + 1)
        .Select(p => new { p.id, p.apellido, p.nombre, p.dni })
        .ToListAsync();

      var hayMas = encontrados.Count > porPagina;

      var results = encontrados
        .Take(porPagina)
        .Select(p => new { id = p.id, text = $"{p.apellido} {p.nombre} - DNI {p.dni}" });

      return Json(new { results, pagination = new { more = hayMas } });
    }

    // valida que la obra social elegida exista
    private async Task ValidarObraSocialAsync(int? obraSocialId)
    {
      if (obraSocialId.HasValue &&
          !await _context.ObrasSociales.AnyAsync(o => o.id == obraSocialId.Value))
      {
        ModelState.AddModelError(nameof(Paciente.obra_social_id), "La obra social seleccionada no existe");
      }
    }

    // deja en la vista el nombre de la obra social elegida para que Select2 la muestre
    private async Task CargarObraSocialAsync(int? obraSocialId)
    {
      if (!obraSocialId.HasValue)
      {
        return;
      }

      ViewBag.ObraSocialNombre = await _context.ObrasSociales
        .AsNoTracking()
        .Where(o => o.id == obraSocialId.Value)
        .Select(o => o.nombre)
        .FirstOrDefaultAsync();
    }
  }
}