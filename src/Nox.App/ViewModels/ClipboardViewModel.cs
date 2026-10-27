using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nox.Core.Abstractions;
using Nox.Core.Clipboard;
using Nox.Core.Text;

namespace Nox.App.ViewModels;

/// <summary>Una fila de la lista: el texto completo y una vista previa de una línea.</summary>
public sealed record ClipboardItem(string Text, string Preview, bool IsPinned);

public partial class ClipboardViewModel : ObservableObject, IDisposable
{
    private const int PreviewLength = 70;

    private readonly IClipboardService _clipboard;
    private readonly PinnedClipsStore _pinnedStore;
    private readonly ClipboardHistory _history = new();
    private readonly Dispatcher _dispatcher;

    [ObservableProperty]
    private bool _hasItems;

    public ClipboardViewModel(IClipboardService clipboard, PinnedClipsStore pinnedStore)
    {
        _clipboard = clipboard;
        _pinnedStore = pinnedStore;
        _dispatcher = Dispatcher.CurrentDispatcher;

        _history.Changed += Rebuild;
        _history.PinnedChanged += SavePinned;
        _history.LoadPinned(_pinnedStore.Load(), DateTimeOffset.Now);
        _clipboard.TextCopied += OnTextCopied;
    }

    public ObservableCollection<ClipboardItem> Items { get; } = [];

    /// <summary>Clic en una fila: vuelve a copiar ese texto (y sube arriba).</summary>
    [RelayCommand]
    private void Copy(ClipboardItem item) => _clipboard.SetText(item.Text);

    [RelayCommand]
    private void TogglePin(ClipboardItem item)
    {
        if (item.IsPinned)
            _history.Unpin(item.Text);
        else
            _history.Pin(item.Text);
    }

    [RelayCommand]
    private void ClearRecent() => _history.ClearRecent();

    private void OnTextCopied(string text) => _dispatcher.BeginInvoke(() => _history.Add(text, DateTimeOffset.Now));

    private void Rebuild()
    {
        Items.Clear();
        foreach (ClipboardEntry entry in _history.Items)
            Items.Add(new ClipboardItem(entry.Text, TextShortener.OneLine(entry.Text, PreviewLength), entry.IsPinned));
        HasItems = Items.Count > 0;
    }

    private void SavePinned()
    {
        try
        {
            _pinnedStore.Save(_history.Pinned.Select(e => e.Text));
        }
        catch (Exception)
        {
            // Sin poder escribir: se quedan fijados en memoria hasta cerrar la app.
        }
    }

    public void Dispose() => _clipboard.TextCopied -= OnTextCopied;
}
