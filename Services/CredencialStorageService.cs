using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace mvc.Services;

public sealed class CredencialStorageService
{
  public const long TamanoMaximo = 2 * 1024 * 1024;

  private static readonly HashSet<string> ExtensionesPermitidas =
      new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

  private const string PrefijoRuta = "/uploads/credenciales/";
  private readonly string _directorio;

  public CredencialStorageService(IWebHostEnvironment entorno)
  {
    if (string.IsNullOrWhiteSpace(entorno.WebRootPath))
    {
      throw new InvalidOperationException("No esta configurado el directorio publico de la aplicacion.");
    }

    _directorio = Path.Combine(entorno.WebRootPath, "uploads", "credenciales");
  }

  // Devuelve el mensaje de error o null si el archivo es valido
  public async Task<string?> ValidarAsync(
      IFormFile? archivo,
      CancellationToken cancellationToken = default)
  {
    if (archivo is null || archivo.Length == 0)
    {
      return "Selecciona una imagen para continuar.";
    }

    if (archivo.Length > TamanoMaximo)
    {
      return "La imagen no puede superar los 2 MB.";
    }

    var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
    if (!ExtensionesPermitidas.Contains(extension))
    {
      return "El formato debe ser JPG, PNG o WebP.";
    }

    await using var flujo = archivo.OpenReadStream();
    var cabecera = await LeerCabeceraAsync(flujo, cancellationToken);
    return CoincideConFormato(extension, cabecera)
        ? null
        : "El contenido no coincide con una imagen JPG, PNG o WebP valida.";
  }

  public async Task<string> GuardarAsync(
      IFormFile archivo,
      CancellationToken cancellationToken = default)
  {
    var error = await ValidarAsync(archivo, cancellationToken);
    if (error is not null)
    {
      throw new InvalidOperationException(error);
    }

    Directory.CreateDirectory(_directorio);
    var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
    var nombreArchivo = $"credencial_{Guid.NewGuid():N}{extension}";
    var rutaAbsoluta = Path.Combine(_directorio, nombreArchivo);
    var archivoCreado = false;

    try
    {
      await using var salida = new FileStream(
          rutaAbsoluta,
          FileMode.CreateNew,
          FileAccess.Write,
          FileShare.None,
          bufferSize: 81920,
          useAsync: true);
      archivoCreado = true;
      await archivo.CopyToAsync(salida, cancellationToken);
    }
    catch
    {
      // Si falla la copia no dejamos un archivo a medias
      if (archivoCreado && File.Exists(rutaAbsoluta))
      {
        File.Delete(rutaAbsoluta);
      }

      throw;
    }

    return PrefijoRuta + nombreArchivo;
  }

  // Borra el archivo solo si la ruta pertenece a esta carpeta
  public void EliminarSiEsGestionada(string? ruta)
  {
    if (string.IsNullOrWhiteSpace(ruta)
        || !ruta.StartsWith(PrefijoRuta, StringComparison.OrdinalIgnoreCase))
    {
      return;
    }

    var nombreArchivo = ruta[PrefijoRuta.Length..];
    if (string.IsNullOrWhiteSpace(nombreArchivo)
        || !string.Equals(Path.GetFileName(nombreArchivo), nombreArchivo, StringComparison.Ordinal))
    {
      return;
    }

    var rutaAbsoluta = Path.GetFullPath(Path.Combine(_directorio, nombreArchivo));
    var directorioGestionado = Path.GetFullPath(_directorio) + Path.DirectorySeparatorChar;
    if (!rutaAbsoluta.StartsWith(directorioGestionado, StringComparison.OrdinalIgnoreCase))
    {
      return;
    }

    if (File.Exists(rutaAbsoluta))
    {
      File.Delete(rutaAbsoluta);
    }
  }

  private static async Task<byte[]> LeerCabeceraAsync(
      Stream flujo,
      CancellationToken cancellationToken)
  {
    var cabecera = new byte[12];
    var totalLeido = 0;
    while (totalLeido < cabecera.Length)
    {
      var leidos = await flujo.ReadAsync(
          cabecera.AsMemory(totalLeido),
          cancellationToken);
      if (leidos == 0)
      {
        break;
      }

      totalLeido += leidos;
    }

    return cabecera[..totalLeido];
  }

  private static bool CoincideConFormato(string extension, byte[] cabecera)
      => extension switch
      {
        ".jpg" or ".jpeg" =>
              cabecera.Length >= 3
              && cabecera[0] == 0xFF
              && cabecera[1] == 0xD8
              && cabecera[2] == 0xFF,
        ".png" =>
              cabecera.Length >= 8
              && cabecera.AsSpan(0, 8).SequenceEqual(new byte[]
              {
                    0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A
              }),
        ".webp" =>
              cabecera.Length >= 12
              && cabecera.AsSpan(0, 4).SequenceEqual("RIFF"u8)
              && cabecera.AsSpan(8, 4).SequenceEqual("WEBP"u8),
        _ => false
      };
}