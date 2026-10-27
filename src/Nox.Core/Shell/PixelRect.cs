namespace Nox.Core.Shell;

/// <summary>Rectángulo en píxeles físicos de pantalla (no en unidades de WPF).</summary>
public readonly record struct PixelRect(int Left, int Top, int Right, int Bottom)
{
    public int Width => Right - Left;
    public int Height => Bottom - Top;

    /// <summary>True si este rectángulo tapa por completo a <paramref name="other"/>.</summary>
    public bool Covers(PixelRect other) =>
        Left <= other.Left && Top <= other.Top && Right >= other.Right && Bottom >= other.Bottom;
}
