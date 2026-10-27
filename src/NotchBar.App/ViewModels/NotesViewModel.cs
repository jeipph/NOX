using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NotchBar.Core.Notes;

namespace NotchBar.App.ViewModels;

/// <summary>Una nota de la lista: dónde está, la primera línea y cuándo se guardó.</summary>
public sealed record NoteItem(string Path, string Preview, string When);

public partial class NotesViewModel : ObservableObject, IDisposable
{
    private const int RecentCount = 10;

    private readonly NotesStore _store;
    private readonly NotchViewModel _options;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _text = "";

    /// <summary>Aviso corto: "Guardada", "No se pudo guardar"...</summary>
    [ObservableProperty]
    private string _status = "";

    public NotesViewModel(NotesStore store, NotchViewModel options)
    {
        _store = store;
        _options = options;
        _options.PropertyChanged += OnOptionsChanged;
        _store.Folder = _options.NotesFolder ?? NotesStore.DefaultFolder;
    }

    public ObservableCollection<NoteItem> Recent { get; } = [];

    /// <summary>Al mostrarse la pestaña se vuelve a leer la carpeta (por si hay notas nuevas de otra app).</summary>
    public void SetVisible(bool isVisible)
    {
        if (isVisible)
            LoadRecent();
        else
            Status = "";
    }

    private bool CanSave() => !string.IsNullOrWhiteSpace(Text);

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        try
        {
            _store.Save(Text, DateTime.Now);
            Text = "";
            Status = "Guardada";
            LoadRecent();
        }
        catch (Exception)
        {
            Status = "No se pudo guardar";
        }
    }

    [RelayCommand]
    private void Open(NoteItem note) => OpenWithWindows(note.Path);

    [RelayCommand]
    private void OpenFolder()
    {
        Directory.CreateDirectory(_store.Folder);
        OpenWithWindows(_store.Folder);
    }

    private void OnOptionsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(NotchViewModel.NotesFolder))
            return;

        _store.Folder = _options.NotesFolder ?? NotesStore.DefaultFolder;
        LoadRecent();
    }

    private void LoadRecent()
    {
        Recent.Clear();
        foreach (NoteFile note in _store.Recent(RecentCount))
            Recent.Add(new NoteItem(note.Path, note.Preview, note.SavedAt.ToString("d MMM, HH:mm")));
    }

    // Abre el archivo o la carpeta con el programa que Windows tenga asignado.
    private void OpenWithWindows(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception)
        {
            Status = "No se pudo abrir";
        }
    }

    public void Dispose() => _options.PropertyChanged -= OnOptionsChanged;
}
