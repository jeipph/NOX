using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nox.App.Services;
using Nox.Core.Hotkeys;

namespace Nox.App.ViewModels;

/// <summary>Una fila de la ventana de atajos: qué hace, el atajo y si funciona.</summary>
public partial class HotkeyRowViewModel(HotkeyAction action) : ObservableObject
{
    public HotkeyAction Action { get; } = action;

    public string Label { get; } = HotkeySettings.Describe(action);

    [ObservableProperty]
    private string _text = "";

    /// <summary>"" si funciona; si no, por qué.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProblem))]
    private string _problem = "";

    public bool HasProblem => Problem.Length > 0;
}

/// <summary>
/// Ventana "Atajos de teclado": cada cambio se guarda al momento (settings.json) y se vuelve a
/// registrar en Windows al salir del cuadro.
/// </summary>
public partial class HotkeysViewModel : ObservableObject, IDisposable
{
    private readonly NotchViewModel _options;
    private readonly HotkeyService _service;

    public HotkeysViewModel(NotchViewModel options, HotkeyService service)
    {
        _options = options;
        _service = service;
        Rows = [.. HotkeySettings.Actions.Select(a => new HotkeyRowViewModel(a))];
        _service.StatusChanged += Refresh;
        Refresh();
    }

    public IReadOnlyList<HotkeyRowViewModel> Rows { get; }

    public bool Enabled
    {
        get => _options.Hotkeys.Enabled;
        set
        {
            _options.Hotkeys = _options.Hotkeys with { Enabled = value };
            OnPropertyChanged();
        }
    }

    /// <summary>Se pulsó una combinación en el cuadro de una fila ("" = quitar el atajo).</summary>
    public void SetHotkey(HotkeyRowViewModel row, string text)
    {
        _options.Hotkeys = _options.Hotkeys.With(row.Action, text);
        row.Text = text;
    }

    public void BeginEditing() => _service.Suspend();

    public void EndEditing() => _service.Resume();

    [RelayCommand]
    private void ResetDefaults()
    {
        _options.Hotkeys = new HotkeySettings { Enabled = _options.Hotkeys.Enabled };
        Refresh();
    }

    private void Refresh()
    {
        foreach (HotkeyRowViewModel row in Rows)
        {
            string text = _options.Hotkeys.Get(row.Action);
            row.Text = Hotkey.Parse(text)?.ToString() ?? text;
            row.Problem = _options.Hotkeys.Enabled && _service.Problems.TryGetValue(row.Action, out string? problem) ? problem : "";
        }
    }

    public void Dispose() => _service.StatusChanged -= Refresh;
}
