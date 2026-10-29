namespace Nox.Core.Diagnostics;

/// <summary>
/// Registro de errores en un archivo de texto (<c>%LOCALAPPDATA%\NOX\logs\error.log</c>).
/// Solo guarda el error técnico (tipo, mensaje y dónde pasó), nunca contenido del usuario.
/// Si pasa de <see cref="MaxBytes"/>, el archivo se aparta como <c>error.old.log</c> y se empieza
/// uno nuevo: nunca crece sin límite.
/// </summary>
public sealed class ErrorLogFile(string? path = null)
{
    public const long MaxBytes = 512 * 1024;

    private readonly Lock _lock = new();

    public string Path { get; } = path ?? System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NOX", "logs", "error.log");

    /// <summary>Apunta un error. Nunca lanza: si no se puede escribir, se pierde en silencio.</summary>
    /// <param name="context">Dónde pasó ("tarea en segundo plano", "interfaz"…).</param>
    public void Write(Exception error, string context, DateTime now)
    {
        try
        {
            lock (_lock)
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
                var file = new FileInfo(Path);
                if (file.Exists && file.Length > MaxBytes)
                    File.Move(Path, OldPath, overwrite: true);
                File.AppendAllText(Path, $"{now:yyyy-MM-dd HH:mm:ss} · {context}{Environment.NewLine}{error}{Environment.NewLine}{Environment.NewLine}");
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Disco lleno o carpeta bloqueada: no se puede hacer nada útil.
        }
    }

    public string OldPath => System.IO.Path.ChangeExtension(Path, ".old.log");
}
