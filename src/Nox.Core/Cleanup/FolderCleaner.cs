namespace Nox.Core.Cleanup;

/// <summary>Lo que ocupa una carpeta y cuánto de eso se podría borrar.</summary>
/// <param name="TotalBytes">Todo lo que ocupan sus archivos (los que se pueden leer).</param>
/// <param name="Files">Cuántos archivos hay.</param>
/// <param name="OldBytes">Lo que ocupan los archivos con más de <see cref="FolderCleaner.MinAge"/>.</param>
/// <param name="OldFiles">Cuántos archivos son así de viejos.</param>
public readonly record struct FolderSize(long TotalBytes, int Files, long OldBytes, int OldFiles);

/// <summary>Resultado de limpiar una carpeta.</summary>
/// <param name="FreedBytes">Lo que ocupaban los archivos borrados.</param>
/// <param name="Deleted">Archivos borrados.</param>
/// <param name="Skipped">Archivos viejos que no se pudieron borrar (en uso o sin permiso).</param>
public readonly record struct CleanResult(long FreedBytes, int Deleted, int Skipped)
{
    public static CleanResult operator +(CleanResult a, CleanResult b) =>
        new(a.FreedBytes + b.FreedBytes, a.Deleted + b.Deleted, a.Skipped + b.Skipped);
}

/// <summary>
/// Mide y limpia carpetas de archivos temporales. Reglas de seguridad:
/// <list type="bullet">
/// <item>Solo borra archivos con más de 24 h (contando la fecha de creación y la de modificación).</item>
/// <item>Nunca sale de la carpeta: no entra en accesos directos de carpeta (enlaces o «junctions»)
/// ni borra nada cuya ruta no esté dentro.</item>
/// <item>Un archivo en uso o sin permiso se salta y se cuenta, sin detener la limpieza.</item>
/// <item>No borra la carpeta en sí; las subcarpetas solo si quedan vacías y son viejas.</item>
/// </list>
/// No usa nada de Windows: sirve igual en las pruebas.
/// </summary>
public sealed class FolderCleaner
{
    /// <summary>Los archivos más nuevos que esto se dejan: puede que una app los esté usando.</summary>
    public static readonly TimeSpan MinAge = TimeSpan.FromHours(24);

    private readonly Action<string> _deleteFile;
    private readonly Action<string> _deleteFolder;

    /// <param name="deleteFile">Para las pruebas (simular un archivo en uso). Por defecto, <see cref="File.Delete"/>.</param>
    public FolderCleaner(Action<string>? deleteFile = null, Action<string>? deleteFolder = null)
    {
        _deleteFile = deleteFile ?? File.Delete;
        _deleteFolder = deleteFolder ?? (path => Directory.Delete(path, recursive: false));
    }

    // Sin entrar en enlaces (podrían llevar fuera de la carpeta) ni pararse en lo que no se puede leer.
    private static EnumerationOptions Walk(bool recurse) => new()
    {
        RecurseSubdirectories = recurse,
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.ReparsePoint,
        ReturnSpecialDirectories = false,
    };

    /// <summary>True si el archivo es lo bastante viejo para borrarlo.</summary>
    public static bool IsOld(DateTime createdUtc, DateTime modifiedUtc, DateTime nowUtc)
    {
        DateTime newest = createdUtc > modifiedUtc ? createdUtc : modifiedUtc;
        return nowUtc - newest >= MinAge;
    }

    /// <summary>
    /// True si parece de verdad una carpeta de temporales: no es la raíz de un disco y su nombre lleva
    /// «temp» o «tmp». Protege de un <c>%TEMP%</c> mal configurado (p. ej. <c>C:\</c>), que haría
    /// borrar archivos viejos de todo el disco.
    /// </summary>
    public static bool IsSafeTempFolder(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;
        string full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        if (IsRoot(full))
            return false;
        string name = Path.GetFileName(full);
        return name.Contains("temp", StringComparison.OrdinalIgnoreCase) || name.Contains("tmp", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRoot(string fullPath) =>
        string.Equals(Path.TrimEndingDirectorySeparator(Path.GetPathRoot(fullPath) ?? ""),
            Path.TrimEndingDirectorySeparator(fullPath), StringComparison.OrdinalIgnoreCase);

    /// <summary>True si <paramref name="path"/> está dentro de <paramref name="root"/> (no es la propia carpeta).</summary>
    public static bool IsInside(string root, string path)
    {
        string fullRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)) + Path.DirectorySeparatorChar;
        string fullPath = Path.GetFullPath(path);
        StringComparison comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return fullPath.Length > fullRoot.Length && fullPath.StartsWith(fullRoot, comparison);
    }

    /// <summary>Cuánto ocupa la carpeta (0 si no existe).</summary>
    public FolderSize Measure(string root, DateTime nowUtc)
    {
        if (!Directory.Exists(root))
            return default;

        long total = 0, old = 0;
        int files = 0, oldFiles = 0;
        foreach (FileInfo file in Files(root))
        {
            try
            {
                long length = file.Length;
                total += length;
                files++;
                if (IsOld(file.CreationTimeUtc, file.LastWriteTimeUtc, nowUtc))
                {
                    old += length;
                    oldFiles++;
                }
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                // Desapareció o no se puede leer mientras se medía: no cuenta.
            }
        }
        return new FolderSize(total, files, old, oldFiles);
    }

    /// <summary>Borra los archivos viejos de la carpeta y luego las subcarpetas viejas que quedaron vacías.</summary>
    public CleanResult Clean(string root, DateTime nowUtc)
    {
        // Nunca la raíz de un disco, pase lo que pase.
        if (!Directory.Exists(root) || IsRoot(Path.GetFullPath(root)))
            return default;

        long freed = 0;
        int deleted = 0, skipped = 0;
        foreach (FileInfo file in Files(root))
        {
            long length;
            try
            {
                if (!IsInside(root, file.FullName) || !IsOld(file.CreationTimeUtc, file.LastWriteTimeUtc, nowUtc))
                    continue;
                length = file.Length;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            try
            {
                if (file.IsReadOnly)
                {
                    skipped++; // marcado «solo lectura» a propósito: se respeta
                    continue;
                }
                _deleteFile(file.FullName);
                freed += length;
                deleted++;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                skipped++; // en uso por otra app o sin permiso
            }
        }

        RemoveEmptyFolders(root, nowUtc);
        return new CleanResult(freed, deleted, skipped);
    }

    private static IEnumerable<FileInfo> Files(string root)
    {
        IEnumerable<FileInfo> files;
        try
        {
            files = new DirectoryInfo(root).EnumerateFiles("*", Walk(recurse: true));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            yield break;
        }

        using IEnumerator<FileInfo> enumerator = files.GetEnumerator();
        while (true)
        {
            FileInfo current;
            try
            {
                if (!enumerator.MoveNext())
                    yield break;
                current = enumerator.Current;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                yield break;
            }
            yield return current;
        }
    }

    // Las más profundas primero, para que al vaciarse una se pueda borrar también la de arriba.
    private void RemoveEmptyFolders(string root, DateTime nowUtc)
    {
        List<DirectoryInfo> folders;
        try
        {
            folders = [.. new DirectoryInfo(root).EnumerateDirectories("*", Walk(recurse: true))];
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            return;
        }

        foreach (DirectoryInfo folder in folders.OrderByDescending(f => f.FullName.Length))
        {
            try
            {
                if (!IsInside(root, folder.FullName) || folder.EnumerateFileSystemInfos().Any())
                    continue;
                folder.Refresh();
                if (IsOld(folder.CreationTimeUtc, DateTime.MinValue, nowUtc))
                    _deleteFolder(folder.FullName);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                // En uso o sin permiso: se deja.
            }
        }
    }
}
