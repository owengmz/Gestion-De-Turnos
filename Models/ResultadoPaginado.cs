using Microsoft.EntityFrameworkCore;

namespace mvc.Models
{
  // resultado paginado generico: sirve para cualquier entidad (T)
  public class ResultadoPaginado<T>
  {
    // datos de la pagina actual
    public List<T> Registros { get; set; } = new List<T>();

    // pagina actual (empieza en 1) y cantidad de registros por pagina
    public int Pagina { get; set; }
    public int RegistrosPorPagina { get; set; }

    // total de registros que cumplen el filtro (no solo los de esta pagina)
    public int TotalRegistros { get; set; }

    // propiedades calculadas: no se guardan, se calculan al leerlas
    public int TotalPaginas => (int)Math.Ceiling((double)TotalRegistros / RegistrosPorPagina);
    public bool TienePaginaAnterior => Pagina > 1;
    public bool TienePaginaSiguiente => Pagina < TotalPaginas;

    // arma el resultado a partir de una consulta
    public static async Task<ResultadoPaginado<T>> CrearAsync(IQueryable<T> consulta, int pagina, int registrosPorPagina)
    {
      // validamos los parametros que llegan del navegador
      if (pagina < 1) pagina = 1;
      if (registrosPorPagina < 1) registrosPorPagina = 10;
      if (registrosPorPagina > 50) registrosPorPagina = 50;

      // primera consulta: cuenta el total (SELECT COUNT)
      int total = await consulta.CountAsync();

      // segunda consulta: trae solo la pagina pedida (OFFSET y LIMIT)
      var registros = await consulta
          .Skip((pagina - 1) * registrosPorPagina)
          .Take(registrosPorPagina)
          .ToListAsync();

      return new ResultadoPaginado<T>
      {
        Registros = registros,
        Pagina = pagina,
        RegistrosPorPagina = registrosPorPagina,
        TotalRegistros = total
      };
    }
  }
}