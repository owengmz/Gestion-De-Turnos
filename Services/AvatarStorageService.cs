using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace mvc.Services;

public sealed class AvatarStorageService
{
    public const long MaxFileSize = 2 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    private const string AvatarPathPrefix = "/uploads/avatars/";
    private readonly string _avatarDirectory;

    public AvatarStorageService(IWebHostEnvironment environment)
    {
        if (string.IsNullOrWhiteSpace(environment.WebRootPath))
        {
            throw new InvalidOperationException("El directorio público de la aplicación no está configurado.");
        }

        _avatarDirectory = Path.Combine(environment.WebRootPath, "uploads", "avatars");
    }

    public async Task<string?> ValidateAsync(
        IFormFile? file,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return "Selecciona una imagen para continuar.";
        }

        if (file.Length > MaxFileSize)
        {
            return "La imagen no puede superar los 2 MB.";
        }

        if (!AllowedExtensions.Contains(Path.GetExtension(file.FileName)))
        {
            return "El formato debe ser JPG, PNG o WebP.";
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        await using var stream = file.OpenReadStream();
        var header = await ReadHeaderAsync(stream, cancellationToken);
        return MatchesImageFormat(extension, header)
            ? null
            : "El contenido no coincide con una imagen JPG, PNG o WebP válida.";
    }

    public async Task<string> SaveAsync(
        int userId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var validationError = await ValidateAsync(file, cancellationToken);
        if (validationError is not null)
        {
            throw new InvalidOperationException(validationError);
        }

        Directory.CreateDirectory(_avatarDirectory);
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"avatar_{userId}_{Guid.NewGuid():N}{extension}";
        var absolutePath = Path.Combine(_avatarDirectory, fileName);
        var fileCreated = false;

        try
        {
            await using var output = new FileStream(
                absolutePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);
            fileCreated = true;
            await file.CopyToAsync(output, cancellationToken);
        }
        catch
        {
            if (fileCreated && File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }

            throw;
        }

        return AvatarPathPrefix + fileName;
    }

    private static async Task<byte[]> ReadHeaderAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        var header = new byte[12];
        var totalRead = 0;
        while (totalRead < header.Length)
        {
            var bytesRead = await stream.ReadAsync(
                header.AsMemory(totalRead),
                cancellationToken);
            if (bytesRead == 0)
            {
                break;
            }

            totalRead += bytesRead;
        }

        return header[..totalRead];
    }

    private static bool MatchesImageFormat(string extension, byte[] header)
        => extension switch
        {
            ".jpg" or ".jpeg" =>
                header.Length >= 3
                && header[0] == 0xFF
                && header[1] == 0xD8
                && header[2] == 0xFF,
            ".png" =>
                header.Length >= 8
                && header.AsSpan(0, 8).SequenceEqual(new byte[]
                {
                    0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A
                }),
            ".webp" =>
                header.Length >= 12
                && header.AsSpan(0, 4).SequenceEqual("RIFF"u8)
                && header.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            _ => false
        };

    public void DeleteIfManaged(string? avatarPath)
    {
        if (string.IsNullOrWhiteSpace(avatarPath)
            || !avatarPath.StartsWith(AvatarPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var fileName = avatarPath[AvatarPathPrefix.Length..];
        if (string.IsNullOrWhiteSpace(fileName)
            || !string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal))
        {
            return;
        }

        var absolutePath = Path.GetFullPath(Path.Combine(_avatarDirectory, fileName));
        var managedDirectory = Path.GetFullPath(_avatarDirectory) + Path.DirectorySeparatorChar;
        if (!absolutePath.StartsWith(managedDirectory, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }
    }
}
