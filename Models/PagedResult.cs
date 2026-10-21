using Microsoft.EntityFrameworkCore;

namespace mvc.Models
{
  // resultado paginado generico: sirve para cualquier entidad (T)
  public class PagedResult<T>
  {
    // datos de la pagina actual
    public List<T> Items { get; set; } = new List<T>();

    // pagina actual (empieza en 1) y cantidad de registros por pagina
    public int Page { get; set; }
    public int PageSize { get; set; }

    // total de registros que cumplen el filtro (no solo los de esta pagina)
    public int TotalItems { get; set; }

    // propiedades calculadas: no se guardan, se calculan al leerlas
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    // arma el resultado a partir de una consulta
    public static async Task<PagedResult<T>> CreateAsync(IQueryable<T> query, int page, int pageSize)
    {
      // validamos los parametros que llegan del navegador
      if (page < 1) page = 1;
      if (pageSize < 1) pageSize = 10;
      if (pageSize > 50) pageSize = 50;

      // primera consulta: cuenta el total (SELECT COUNT)
      int total = await query.CountAsync();

      // segunda consulta: trae solo la pagina pedida (OFFSET y LIMIT)
      var items = await query
          .Skip((page - 1) * pageSize)
          .Take(pageSize)
          .ToListAsync();

      return new PagedResult<T>
      {
        Items = items,
        Page = page,
        PageSize = pageSize,
        TotalItems = total
      };
    }
  }
}