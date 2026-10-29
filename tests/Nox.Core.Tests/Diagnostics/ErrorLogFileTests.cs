using Nox.Core.Diagnostics;

namespace Nox.Core.Tests.Diagnostics;

public sealed class ErrorLogFileTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "NOXTests", Guid.NewGuid().ToString("N"));
    private static readonly DateTime Now = new(2026, 10, 29, 16, 30, 0);

    private ErrorLogFile NewLog() => new(Path.Combine(_folder, "logs", "error.log"));

    [Fact]
    public void Write_CreatesTheFolder_AndAppends()
    {
        ErrorLogFile log = NewLog();

        log.Write(new InvalidOperationException("primero"), "interfaz", Now);
        log.Write(new TimeoutException("segundo"), "tarea en segundo plano", Now.AddMinutes(1));

        string text = File.ReadAllText(log.Path);
        Assert.Contains("2026-10-29 16:30:00 · interfaz", text);
        Assert.Contains("InvalidOperationException: primero", text);
        Assert.Contains("TimeoutException: segundo", text);
    }

    [Fact]
    public void BigFile_IsSetAside_AndANewOneStarts()
    {
        ErrorLogFile log = NewLog();
        Directory.CreateDirectory(Path.GetDirectoryName(log.Path)!);
        File.WriteAllText(log.Path, new string('x', (int)ErrorLogFile.MaxBytes + 10));

        log.Write(new Exception("nuevo"), "interfaz", Now);

        Assert.True(File.Exists(log.OldPath));
        Assert.True(new FileInfo(log.Path).Length < 2000);
        Assert.Contains("nuevo", File.ReadAllText(log.Path));
    }

    [Fact]
    public void LockedFile_DoesNotThrow()
    {
        ErrorLogFile log = NewLog();
        Directory.CreateDirectory(Path.GetDirectoryName(log.Path)!);
        using var locked = new FileStream(log.Path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

        log.Write(new Exception("no se puede escribir"), "interfaz", Now); // no debe lanzar
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }
}
