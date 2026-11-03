using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nox.Core.Abstractions;
using Nox.Core.Cleanup;

namespace Nox.App.ViewModels;

/// <summary>
/// Ventana «Limpiar caché y RAM». Todo el trabajo (medir, borrar, liberar) va en segundo plano para
/// que la ventana no se congele. Solo toca dos carpetas: <c>%TEMP%</c> y la caché de letras de NOX,
/// y en ellas solo archivos con más de 24 h. No toca la Papelera.
/// </summary>
public partial class CleanupViewModel : ObservableObject
{
    private readonly ISystemMemory _memory;
    private readonly TimeSpan _ramSettleDelay;
    private readonly FolderCleaner _cleaner = new();
    private readonly string? _tempFolder;
    private readonly string _cacheFolder;

    public CleanupViewModel(ISystemMemory memory, TimeSpan ramSettleDelay)
    {
        _memory = memory;
        _ramSettleDelay = ramSettleDelay;
        string temp = Path.GetTempPath();
        // Si %TEMP% apunta a algo que no es una carpeta de temporales (p. ej. C:\), no se toca.
        _tempFolder = FolderCleaner.IsSafeTempFolder(temp) ? temp : null;
        _cacheFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NOX", "lyrics");
    }

    [ObservableProperty]
    private string _tempText = "Calculando…";

    [ObservableProperty]
    private string _cacheText = "Calculando…";

    [ObservableProperty]
    private string _filesResult = "";

    [ObservableProperty]
    private string _ramText = "Calculando…";

    [ObservableProperty]
    private string _ramResult = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CleanFilesCommand), nameof(FreeRamCommand))]
    private bool _isBusy;

    private bool CanRun => !IsBusy;

    /// <summary>Al abrir la ventana: mide las carpetas y la RAM.</summary>
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            await MeasureFilesAsync();
            ShowRam(await Task.Run(_memory.GetStatus));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task CleanFilesAsync()
    {
        IsBusy = true;
        FilesResult = "Limpiando…";
        try
        {
            CleanResult result = await Task.Run(() =>
            {
                DateTime now = DateTime.UtcNow;
                CleanResult total = _cleaner.Clean(_cacheFolder, now);
                if (_tempFolder is not null)
                    total += _cleaner.Clean(_tempFolder, now);
                return total;
            });
            FilesResult = result.Deleted == 0 && result.Skipped == 0
                ? "No había archivos con más de 24 h."
                : $"Liberados {SizeText.Megabytes(result.FreedBytes)} ({result.Deleted} archivos)."
                  + (result.Skipped > 0 ? $" {result.Skipped} estaban en uso y se dejaron." : "");
            await MeasureFilesAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task FreeRamAsync()
    {
        IsBusy = true;
        RamResult = "Liberando…";
        try
        {
            MemoryStatus before = await Task.Run(_memory.GetStatus);
            int processes = await Task.Run(_memory.TrimWorkingSets);
            await Task.Delay(_ramSettleDelay); // Windows tarda un poco en contar la memoria devuelta
            MemoryStatus after = await Task.Run(_memory.GetStatus);
            ShowRam(after);
            long freed = before.UsedBytes - after.UsedBytes;
            RamResult = $"Antes: {SizeText.Megabytes(before.UsedBytes)} en uso. Después: {SizeText.Megabytes(after.UsedBytes)}"
                + (freed > 0 ? $" (se liberaron {SizeText.Megabytes(freed)} de {processes} apps)." : ".");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task MeasureFilesAsync()
    {
        var (temp, cache) = await Task.Run(() =>
        {
            DateTime now = DateTime.UtcNow;
            FolderSize tempSize = _tempFolder is null ? default : _cleaner.Measure(_tempFolder, now);
            return (tempSize, _cleaner.Measure(_cacheFolder, now));
        });
        TempText = _tempFolder is null
            ? "La carpeta %TEMP% no parece de temporales: no se toca."
            : Describe(temp);
        CacheText = Describe(cache);
    }

    private static string Describe(FolderSize size) =>
        $"{SizeText.Megabytes(size.TotalBytes)} ({SizeText.Megabytes(size.OldBytes)} con más de 24 h)";

    private void ShowRam(MemoryStatus status) =>
        RamText = $"{SizeText.Megabytes(status.UsedBytes)} en uso de {SizeText.Megabytes(status.TotalBytes)}";
}
