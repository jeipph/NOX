using Nox.Core.Cleanup;

namespace Nox.Core.Tests.Cleanup;

public sealed class FolderCleanerTests : IDisposable
{
    private static readonly DateTime Now = DateTime.UtcNow;

    private readonly string _root = Path.Combine(Path.GetTempPath(), "NOXTests", Guid.NewGuid().ToString("N"), "temp");
    private readonly string _outside;

    public FolderCleanerTests()
    {
        Directory.CreateDirectory(_root);
        _outside = Path.Combine(Path.GetDirectoryName(_root)!, "fuera");
        Directory.CreateDirectory(_outside);
    }

    private string MakeFile(string relative, int bytes, double hoursOld, string? root = null)
    {
        string path = Path.Combine(root ?? _root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, new byte[bytes]);
        DateTime when = Now.AddHours(-hoursOld);
        File.SetCreationTimeUtc(path, when);
        File.SetLastWriteTimeUtc(path, when);
        return path;
    }

    [Theory]
    [InlineData(25, 25, true)]
    [InlineData(24, 30, true)]
    [InlineData(30, 2, false)]  // modificado hace poco
    [InlineData(2, 30, false)]  // copiado hace poco (fecha de creación nueva)
    public void IsOld_NeedsBothDatesOlderThan24Hours(double createdHoursAgo, double modifiedHoursAgo, bool expected) =>
        Assert.Equal(expected, FolderCleaner.IsOld(Now.AddHours(-createdHoursAgo), Now.AddHours(-modifiedHoursAgo), Now));

    [Fact]
    public void Measure_CountsEverything_AndWhatIsOld()
    {
        MakeFile("viejo.tmp", 1000, 48);
        MakeFile(Path.Combine("sub", "viejo2.tmp"), 500, 30);
        MakeFile("nuevo.tmp", 200, 1);

        FolderSize size = new FolderCleaner().Measure(_root, Now);

        Assert.Equal(new FolderSize(1700, 3, 1500, 2), size);
    }

    [Fact]
    public void Measure_MissingFolder_IsZero() =>
        Assert.Equal(default, new FolderCleaner().Measure(Path.Combine(_root, "no-existe"), Now));

    [Fact]
    public void Clean_DeletesOnlyOldFiles_AndSaysHowMuch()
    {
        string old = MakeFile("viejo.tmp", 1000, 48);
        string nested = MakeFile(Path.Combine("a", "b", "viejo.log"), 300, 72);
        string recent = MakeFile("nuevo.tmp", 200, 1);

        CleanResult result = new FolderCleaner().Clean(_root, Now);

        Assert.Equal(new CleanResult(1300, 2, 0), result);
        Assert.False(File.Exists(old));
        Assert.False(File.Exists(nested));
        Assert.True(File.Exists(recent));
        Assert.True(Directory.Exists(_root)); // la carpeta en sí no se borra
    }

    [Fact]
    public void Clean_FileInUse_IsSkipped_AndTheRestGoesOn()
    {
        string busy = MakeFile("en-uso.tmp", 400, 48);
        string free = MakeFile("libre.tmp", 100, 48);
        var cleaner = new FolderCleaner(deleteFile: path =>
        {
            if (path == busy)
                throw new IOException("El archivo está en uso por otro proceso.");
            File.Delete(path);
        });

        CleanResult result = cleaner.Clean(_root, Now);

        Assert.Equal(new CleanResult(100, 1, 1), result);
        Assert.True(File.Exists(busy));
        Assert.False(File.Exists(free));
    }

    [Fact]
    public void Clean_ReadOnlyFile_IsLeftAlone()
    {
        string path = MakeFile("solo-lectura.tmp", 100, 48);
        File.SetAttributes(path, FileAttributes.ReadOnly);

        CleanResult result = new FolderCleaner().Clean(_root, Now);

        Assert.Equal(new CleanResult(0, 0, 1), result);
        Assert.True(File.Exists(path));
        File.SetAttributes(path, FileAttributes.Normal);
    }

    [Fact]
    public void Clean_DoesNotFollowLinks_OutOfTheFolder()
    {
        string precious = MakeFile("importante.txt", 100, 100, _outside);
        try
        {
            Directory.CreateSymbolicLink(Path.Combine(_root, "enlace"), _outside);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            return; // en Windows sin permiso para crear enlaces no se puede probar
        }

        CleanResult result = new FolderCleaner().Clean(_root, Now);
        FolderSize size = new FolderCleaner().Measure(_root, Now);

        Assert.True(File.Exists(precious));
        Assert.Equal(0, result.Deleted);
        Assert.Equal(0, size.Files);
    }

    [Fact]
    public void Clean_RemovesEmptyOldFolders_ButNotNewOnes()
    {
        // Ya vacía (en Linux la fecha de creación de una carpeta cambia al borrar lo que tiene dentro).
        string oldFolder = Path.Combine(_root, "vieja");
        Directory.CreateDirectory(oldFolder);
        Directory.SetCreationTimeUtc(oldFolder, Now.AddHours(-48));
        Directory.SetLastWriteTimeUtc(oldFolder, Now.AddHours(-48));
        string newFolder = Path.Combine(_root, "recien-creada");
        Directory.CreateDirectory(newFolder);

        new FolderCleaner().Clean(_root, Now);

        Assert.False(Directory.Exists(oldFolder));
        Assert.True(Directory.Exists(newFolder));
    }

    [Theory]
    [InlineData("hijo.txt", true)]
    [InlineData("sub/nieto.txt", true)]
    [InlineData("../fuera/otro.txt", false)]
    [InlineData("", false)] // la propia carpeta
    public void IsInside_OnlyBelowTheRoot(string relative, bool expected) =>
        Assert.Equal(expected, FolderCleaner.IsInside(_root, Path.Combine(_root, relative)));

    [Fact]
    public void IsInside_FolderWithSimilarName_IsOutside() =>
        Assert.False(FolderCleaner.IsInside(_root, _root + "-copia" + Path.DirectorySeparatorChar + "a.txt"));

    [Fact]
    public void IsSafeTempFolder_OnlyRealTempFolders()
    {
        Assert.True(FolderCleaner.IsSafeTempFolder(Path.GetTempPath())); // la de verdad (Temp en Windows, tmp en Linux)
        Assert.True(FolderCleaner.IsSafeTempFolder(Path.Combine(_root, "Temp")));
        Assert.True(FolderCleaner.IsSafeTempFolder(Path.Combine(_root, "tmp") + Path.DirectorySeparatorChar));
        Assert.False(FolderCleaner.IsSafeTempFolder(Path.GetPathRoot(_root)));
        Assert.False(FolderCleaner.IsSafeTempFolder(Path.Combine(_root, "Documentos")));
        Assert.False(FolderCleaner.IsSafeTempFolder(""));
        Assert.False(FolderCleaner.IsSafeTempFolder(null));
    }

    [Fact]
    public void Clean_NeverTouchesADriveRoot() =>
        Assert.Equal(default, new FolderCleaner(deleteFile: _ => throw new InvalidOperationException("no debía borrar"))
            .Clean(Path.GetPathRoot(_root)!, Now));

    [Fact]
    public void CleanResults_AddUp() =>
        Assert.Equal(new CleanResult(30, 3, 1), new CleanResult(10, 1, 0) + new CleanResult(20, 2, 1));

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path.GetDirectoryName(_root)!, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}
