using System.Windows.Media;
using System.Windows.Media.Imaging;
using Nox.Core.Pets;

namespace Nox.App.Services;

/// <summary>
/// Las imágenes de las mascotas y juguetes, creadas una vez y reutilizadas (pestaña Mascota,
/// aviso de descanso y notch cerrado). Se amplían en pantalla con NearestNeighbor.
/// </summary>
public static class PetBitmaps
{
    private static readonly Dictionary<(PetKind, PetPose), BitmapSource> Pets = [];
    private static readonly Dictionary<(PetToy, int), BitmapSource> Toys = [];

    public static BitmapSource Pet(PetKind kind, PetPose pose)
    {
        if (!Pets.TryGetValue((kind, pose), out BitmapSource? frame))
            Pets[(kind, pose)] = frame = ToBitmap(PetSprites.Frame(kind, pose), PetSprites.Palette(kind));
        return frame;
    }

    public static BitmapSource Toy(PetToy toy, int index)
    {
        index %= PetSprites.ToyFrameCount(toy);
        if (!Toys.TryGetValue((toy, index), out BitmapSource? frame))
            Toys[(toy, index)] = frame = ToBitmap(PetSprites.Toy(toy, index), PetSprites.ToyPalette(toy));
        return frame;
    }

    // ARGB en un uint queda en memoria como B, G, R, A: justo el formato Bgra32.
    private static BitmapSource ToBitmap(IReadOnlyList<string> rows, IReadOnlyDictionary<char, uint> palette)
    {
        int width = rows[0].Length;
        uint[] pixels = PetSprites.Render(rows, palette);
        var bitmap = BitmapSource.Create(width, rows.Count, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4);
        bitmap.Freeze();
        return bitmap;
    }
}
