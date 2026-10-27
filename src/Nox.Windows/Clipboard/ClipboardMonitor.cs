using System.Runtime.InteropServices;
using Nox.Core.Abstractions;
using static Nox.Windows.Shell.NativeMethods;

namespace Nox.Windows.Clipboard;

/// <summary>
/// Escucha el portapapeles con <c>AddClipboardFormatListener</c>: Windows manda WM_CLIPBOARDUPDATE
/// a la ventana del notch cada vez que algo se copia. Sin sondeo.
/// Nunca escribe el contenido en registros (logs).
/// </summary>
public sealed class ClipboardMonitor : IClipboardService, IDisposable
{
    private const int OpenAttempts = 5;
    private static readonly TimeSpan RetryWait = TimeSpan.FromMilliseconds(40);

    // Marcas que ponen los gestores de contraseñas (y otras apps) para pedir que no se guarde la copia.
    private readonly uint _excludeFormat = RegisterClipboardFormat("ExcludeClipboardContentFromMonitorProcessing");
    private readonly uint _historyFormat = RegisterClipboardFormat("CanIncludeInClipboardHistory");
    private nint _hwnd;

    public event Action<string>? TextCopied;

    /// <summary>Empieza a escuchar con la ventana dada (sus mensajes deben pasar por <see cref="HandleMessage"/>).</summary>
    public void Attach(nint hwnd)
    {
        _hwnd = hwnd;
        AddClipboardFormatListener(hwnd);
    }

    /// <summary>Llamar desde el WndProc. Devuelve true si el mensaje era del portapapeles.</summary>
    public bool HandleMessage(int msg)
    {
        if (msg != WM_CLIPBOARDUPDATE)
            return false;

        _ = ReadAsync();
        return true;
    }

    // Otra app puede tener el portapapeles abierto un momento: se reintenta unas pocas veces.
    private async Task ReadAsync()
    {
        for (int attempt = 0; attempt < OpenAttempts; attempt++)
        {
            if (OpenClipboard(_hwnd))
            {
                string? text;
                try
                {
                    text = ReadAllowedText();
                }
                finally
                {
                    CloseClipboard();
                }

                if (text is not null)
                    TextCopied?.Invoke(text);
                return;
            }
            await Task.Delay(RetryWait);
        }
    }

    private string? ReadAllowedText()
    {
        if (IsClipboardFormatAvailable(_excludeFormat))
            return null;

        if (IsClipboardFormatAvailable(_historyFormat) && ReadInt32(_historyFormat) == 0)
            return null;

        if (!IsClipboardFormatAvailable(CF_UNICODETEXT))
            return null;

        nint handle = GetClipboardData(CF_UNICODETEXT);
        nint pointer = handle == 0 ? 0 : GlobalLock(handle);
        if (pointer == 0)
            return null;

        try
        {
            return Marshal.PtrToStringUni(pointer);
        }
        finally
        {
            GlobalUnlock(handle);
        }
    }

    private static int? ReadInt32(uint format)
    {
        nint handle = GetClipboardData(format);
        if (handle == 0 || GlobalSize(handle) < sizeof(int))
            return null;

        nint pointer = GlobalLock(handle);
        if (pointer == 0)
            return null;

        try
        {
            return Marshal.ReadInt32(pointer);
        }
        finally
        {
            GlobalUnlock(handle);
        }
    }

    public bool SetText(string text)
    {
        for (int attempt = 0; attempt < OpenAttempts; attempt++)
        {
            if (OpenClipboard(_hwnd))
            {
                try
                {
                    EmptyClipboard();
                    return WriteText(text);
                }
                finally
                {
                    CloseClipboard();
                }
            }
            Thread.Sleep(RetryWait);
        }
        return false;
    }

    private static bool WriteText(string text)
    {
        // Texto UTF-16 terminado en cero, en un bloque de memoria que pasa a ser de Windows.
        nuint bytes = (nuint)((text.Length + 1) * sizeof(char));
        nint handle = GlobalAlloc(GMEM_MOVEABLE, bytes);
        if (handle == 0)
            return false;

        nint pointer = GlobalLock(handle);
        if (pointer == 0)
        {
            GlobalFree(handle);
            return false;
        }

        try
        {
            Marshal.Copy(text.ToCharArray(), 0, pointer, text.Length);
            Marshal.WriteInt16(pointer, text.Length * sizeof(char), 0);
        }
        finally
        {
            GlobalUnlock(handle);
        }

        if (SetClipboardData(CF_UNICODETEXT, handle) != 0)
            return true;

        GlobalFree(handle); // Si Windows no la aceptó, la memoria sigue siendo nuestra.
        return false;
    }

    public void Dispose()
    {
        if (_hwnd != 0)
            RemoveClipboardFormatListener(_hwnd);
        _hwnd = 0;
    }
}
