namespace Nox.Core.Pets;

/// <summary>Mascotas que se pueden elegir.</summary>
public enum PetKind
{
    /// <summary>Gato gris oscuro de ojos amarillos.</summary>
    GrayCat,

    /// <summary>Gato amarillo atigrado.</summary>
    TabbyCat,

    Beagle,
}

/// <summary>Las posturas de la mascota (una imagen cada una).</summary>
public enum PetPose
{
    Sit,
    SitBlink,
    StandA,
    StandB,
    Crouch,
    Jump,
    Swipe,
}

/// <summary>
/// Dibujos en pixel art de las mascotas (propios de NOX), mirando a la derecha.
/// Cada letra es un color de la paleta y '.' es transparente. Los dos gatos comparten dibujo
/// y cambian de paleta; el beagle tiene el suyo.
/// </summary>
public static class PetSprites
{
    public const int Width = 20;
    public const int Height = 14;
    public const int BallSize = 5;

    // Gato: o contorno, b pelo, s rayas, l claro (hocico, panza), e ojo, p pupila, n nariz.
    private static readonly string[] CatHead =
    [
        ".............o...o..",
        "............obo.obo.",
        "............obbbbbo.",
        "............oepbepo.",
        ".o..........obbnbbo.",
        "obo.........oblllbo.",
    ];

    private static readonly string[] CatBody =
    [
        ".obooooooooobbbbbo..",
        "..obsbbsbbsbbbbbbo..",
        "...obsbbsbbsbbbbbo..",
        "...obbbbbbbbbbllbo..",
        "...obbbbbbbbbbbllo..",
    ];

    private static readonly string[] CatSit =
    [
        ".............o...o..",
        "............obo.obo.",
        "............obbbbbo.",
        "............oepbepo.",
        "............obbnbbo.",
        "............oblllbo.",
        "...........obbbbbbo.",
        "..........obsbbbllo.",
        "..........obbsbbllo.",
        "..........osbbsbllo.",
        "..o.......obbbbbllo.",
        ".obo......obbbbbllo.",
        "..obooooooobbbbbllo.",
        "...........olloollo.",
    ];

    // Beagle: o contorno, w blanco, t canela, k manto negro, r oreja, e ojo, n nariz.
    private static readonly string[] DogHead =
    [
        "..o.........oooo....",
        ".owo.......otttto...",
        ".oto.......ottetoo..",
        "..oto......orrttwwo.",
        "...ot......orrtwwwno",
    ];

    private static readonly string[] DogBody =
    [
        "....oooooooorrwwwwo.",
        "...otkkkkkkkrrwwwo..",
        "...ottkkkkkkkrwwwo..",
        "...otttkkkkktttwwo..",
        "...otttttttttttwwo..",
        "...owwwwwwwwwwwwwo..",
    ];

    private static readonly string[] DogSit =
    [
        "............oooo....",
        "...........otttto...",
        "...........ottetoo..",
        "...........orrttwwo.",
        "...........orrtwwwno",
        "...........orrwwwwo.",
        "..........okrrwwwo..",
        "..........okkkwwwo..",
        "..........okkttwwo..",
        ".o........ottttwwo..",
        "owo.......ottttwwo..",
        ".oto......ottttwwo..",
        "..otoooooootttwwwo..",
        "..........owwoowwo..",
    ];

    // Ovillo: o contorno, y lana, k hebra.
    private static readonly string[][] BallFrames =
    [
        [".ooo.", "oykyo", "okyko", "oykyo", ".ooo."],
        [".ooo.", "okyyo", "oykyo", "oyyko", ".ooo."],
        [".ooo.", "oyyko", "oykyo", "okyyo", ".ooo."],
    ];

    public static int BallFrameCount => BallFrames.Length;

    public static IReadOnlyList<string> Ball(int frame) => BallFrames[((frame % BallFrames.Length) + BallFrames.Length) % BallFrames.Length];

    /// <summary>Las filas del dibujo de esa mascota en esa postura.</summary>
    public static IReadOnlyList<string> Frame(PetKind kind, PetPose pose) =>
        kind == PetKind.Beagle ? DogFrame(pose) : CatFrame(pose);

    private static string[] CatFrame(PetPose pose) => pose switch
    {
        PetPose.Sit => CatSit,
        PetPose.SitBlink => Replace(CatSit, 3, "............ooobooo."),
        PetPose.StandA => [.. CatHead, .. CatBody, .. Legs('b', 'l', apart: true)],
        PetPose.StandB => [.. CatHead, .. CatBody, .. Legs('b', 'l', apart: false)],
        // Agazapado: el cuerpo baja dos píxeles y las patas se doblan.
        PetPose.Crouch => [Empty, Empty, .. CatHead, .. CatBody, "...olo.olo...olo.olo"],
        PetPose.Jump => [.. CatHead, .. CatBody, .. JumpLegs('b')],
        _ => [.. CatHead, .. CatBody[..3], "...obbbbbbbbbbllbool", CatBody[4], .. SwipeLegs('b', 'l')],
    };

    private static string[] DogFrame(PetPose pose) => pose switch
    {
        PetPose.Sit => DogSit,
        PetPose.SitBlink => Replace(DogSit, 2, "...........ottotoo.."),
        PetPose.StandA => [.. DogHead, .. DogBody, .. Legs('w', 'w', apart: true)],
        PetPose.StandB => [.. DogHead, .. DogBody, .. Legs('w', 'w', apart: false)],
        PetPose.Crouch => [Empty, Empty, .. DogHead, .. DogBody, "...owo.owo...owo.owo"],
        PetPose.Jump => [.. DogHead, .. DogBody, .. JumpLegs('w')],
        _ => [.. DogHead, .. DogBody[..4], "...otttttttttttwwoow", DogBody[5], .. SwipeLegs('w', 'w')],
    };

    private static readonly string Empty = new('.', Width);

    // Tres filas de patas: separadas (paso) o juntas (el otro paso). "paw" es el color de la punta.
    private static string[] Legs(char fur, char paw, bool apart) => apart
        ? [$"...o{fur}o.o{fur}o...o{fur}o.o{fur}o", $"...o{fur}o.o{fur}o...o{fur}o.o{fur}o", $"...o{paw}o.o{paw}o...o{paw}o.o{paw}o"]
        : [$"....o{fur}oo{fur}o..o{fur}oo{fur}o..", $"....o{fur}oo{fur}o..o{fur}oo{fur}o..", $"....o{paw}oo{paw}o..o{paw}oo{paw}o.."];

    // En el aire: patas de atrás hacia atrás y las de delante hacia delante.
    private static string[] JumpLegs(char fur) =>
        [$"..o{fur}o...........o{fur}o.", $".o{fur}o.............o{fur}o", ".oo..............oo."];

    // Dando un zarpazo: la pata de delante está levantada (dibujada en el cuerpo).
    private static string[] SwipeLegs(char fur, char paw) =>
        [$"...o{fur}o.o{fur}o...o{fur}o....", $"...o{fur}o.o{fur}o...o{fur}o....", $"...o{paw}o.o{paw}o...o{paw}o...."];

    private static string[] Replace(string[] rows, int index, string row)
    {
        string[] copy = [.. rows];
        copy[index] = row;
        return copy;
    }

    /// <summary>Colores (ARGB) de cada letra del dibujo.</summary>
    public static IReadOnlyDictionary<char, uint> Palette(PetKind kind) => kind switch
    {
        PetKind.GrayCat => new Dictionary<char, uint>
        {
            // Gris oscuro, pero no tanto que desaparezca sobre el notch negro.
            ['o'] = 0xFF1C1C21, ['b'] = 0xFF5A5A65, ['s'] = 0xFF474751, ['l'] = 0xFF888894,
            ['e'] = 0xFFF7C51E, ['p'] = 0xFF101010, ['n'] = 0xFFE597AC,
        },
        PetKind.TabbyCat => new Dictionary<char, uint>
        {
            ['o'] = 0xFF3D2410, ['b'] = 0xFFF2B33D, ['s'] = 0xFFC2721A, ['l'] = 0xFFFBE4AA,
            ['e'] = 0xFF7CC243, ['p'] = 0xFF101010, ['n'] = 0xFFE597AC,
        },
        _ => new Dictionary<char, uint>
        {
            ['o'] = 0xFF2A1A10, ['w'] = 0xFFF4EFE6, ['t'] = 0xFFC98B4A, ['k'] = 0xFF2B2B2B,
            ['r'] = 0xFF7A4A26, ['e'] = 0xFF2E1B10, ['n'] = 0xFF111111,
        },
    };

    public static IReadOnlyDictionary<char, uint> BallPalette { get; } = new Dictionary<char, uint>
    {
        ['o'] = 0xFF6B2D3F, ['y'] = 0xFFE0607E, ['k'] = 0xFFB03A57,
    };

    /// <summary>
    /// Convierte el dibujo en píxeles ARGB (fila tras fila), listos para hacer una imagen.
    /// '.' es transparente (0).
    /// </summary>
    /// <exception cref="ArgumentException">Una letra no está en la paleta.</exception>
    public static uint[] Render(IReadOnlyList<string> rows, IReadOnlyDictionary<char, uint> palette)
    {
        int width = rows[0].Length;
        var pixels = new uint[width * rows.Count];
        for (int y = 0; y < rows.Count; y++)
        {
            for (int x = 0; x < width; x++)
            {
                char c = rows[y][x];
                if (c == '.')
                    continue;
                pixels[y * width + x] = palette.TryGetValue(c, out uint color)
                    ? color
                    : throw new ArgumentException($"El color '{c}' no está en la paleta (fila {y}, columna {x}).");
            }
        }
        return pixels;
    }
}
