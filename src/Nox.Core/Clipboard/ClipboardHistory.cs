namespace Nox.Core.Clipboard;

/// <summary>Un texto copiado.</summary>
public sealed record ClipboardEntry(string Text, DateTimeOffset CopiedAt, bool IsPinned);

/// <summary>
/// Historial del portapapeles: los fijados primero y luego las copias recientes (la más nueva arriba).
/// Cada texto aparece una sola vez. Las recientes tienen un máximo; las fijadas no.
/// </summary>
public sealed class ClipboardHistory
{
    public const int MaxRecent = 50;

    private readonly List<ClipboardEntry> _pinned = [];
    private readonly List<ClipboardEntry> _recent = [];

    /// <summary>Cambió la lista (para redibujar).</summary>
    public event Action? Changed;

    /// <summary>Cambiaron los fijados (para guardarlos en disco).</summary>
    public event Action? PinnedChanged;

    public IReadOnlyList<ClipboardEntry> Items => [.. _pinned, .. _recent];

    public IReadOnlyList<ClipboardEntry> Pinned => _pinned;

    /// <summary>Añade una copia. Si el texto ya estaba entre las recientes, sube arriba. Devuelve false si no cambia nada.</summary>
    public bool Add(string text, DateTimeOffset copiedAt)
    {
        if (string.IsNullOrWhiteSpace(text) || _pinned.Exists(e => e.Text == text))
            return false;
        if (_recent.Count > 0 && _recent[0].Text == text)
            return false;

        _recent.RemoveAll(e => e.Text == text);
        _recent.Insert(0, new ClipboardEntry(text, copiedAt, IsPinned: false));
        if (_recent.Count > MaxRecent)
            _recent.RemoveRange(MaxRecent, _recent.Count - MaxRecent);

        Changed?.Invoke();
        return true;
    }

    public void Pin(string text)
    {
        int index = _recent.FindIndex(e => e.Text == text);
        if (index < 0)
            return;

        ClipboardEntry entry = _recent[index];
        _recent.RemoveAt(index);
        _pinned.Insert(0, entry with { IsPinned = true });
        Changed?.Invoke();
        PinnedChanged?.Invoke();
    }

    public void Unpin(string text)
    {
        int index = _pinned.FindIndex(e => e.Text == text);
        if (index < 0)
            return;

        ClipboardEntry entry = _pinned[index];
        _pinned.RemoveAt(index);
        _recent.Insert(0, entry with { IsPinned = false });
        if (_recent.Count > MaxRecent)
            _recent.RemoveRange(MaxRecent, _recent.Count - MaxRecent);
        Changed?.Invoke();
        PinnedChanged?.Invoke();
    }

    /// <summary>Borra las copias recientes; los fijados se quedan.</summary>
    public void ClearRecent()
    {
        if (_recent.Count == 0)
            return;

        _recent.Clear();
        Changed?.Invoke();
    }

    /// <summary>Carga los fijados guardados al arrancar.</summary>
    public void LoadPinned(IEnumerable<string> texts, DateTimeOffset loadedAt)
    {
        _pinned.Clear();
        foreach (string text in texts.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct())
            _pinned.Add(new ClipboardEntry(text, loadedAt, IsPinned: true));
        Changed?.Invoke();
    }
}
