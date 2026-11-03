using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nox.Core.Pets;

namespace Nox.App.ViewModels;

/// <summary>Ventanita «Otro…»: escribir cada cuántos minutos avisar de descansar (de 5 a 480).</summary>
public partial class CustomBreakViewModel(NotchViewModel options) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _error = "";

    /// <summary>Lo escrito; empieza con el intervalo actual (o 40 si está en «Nunca»).</summary>
    [ObservableProperty]
    private string _text = (options.BreakMinutes > 0 ? options.BreakMinutes : 40).ToString();

    public bool HasError => Error.Length > 0;

    public string Hint => $"Entre {BreakIntervals.MinMinutes} y {BreakIntervals.MaxMinutes} minutos.";

    /// <summary>Se aceptó un valor válido: la ventana se cierra.</summary>
    public event Action? Accepted;

    partial void OnTextChanged(string value) => Error = "";

    [RelayCommand]
    private void Accept()
    {
        if (!BreakIntervals.TryParse(Text, out int minutes, out string error))
        {
            Error = error;
            return;
        }
        options.BreakMinutes = minutes;
        Accepted?.Invoke();
    }
}
