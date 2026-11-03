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

/// <summary>Juguete con el que juega la mascota.</summary>
public enum PetToy
{
    /// <summary>Ovillo de lana: rueda y se para pronto.</summary>
    Yarn,

    /// <summary>Pelota: bota y rueda lejos.</summary>
    Ball,

    /// <summary>Ratón de juguete: de vez en cuando se escapa solo.</summary>
    Mouse,
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
    SleepA,
    SleepB,
    Stretch,
    LickA,
    LickB,

    /// <summary>Maullando o ladrando: sentada con la boca abierta.</summary>
    Talk,

    /// <summary>Comiendo con la cabeza baja (dos imágenes que se alternan al masticar).</summary>
    EatA,
    EatB,
}

/// <summary>Bocadillo de texto sobre la cabeza de la mascota.</summary>
public enum PetBubble
{
    None,

    /// <summary>«¡Miau!» o «¡Guau!».</summary>
    Talk,

    /// <summary>«¡Tengo hambre!».</summary>
    Hungry,
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
    public const int ToySize = 5;

    /// <summary>Cuenco de comida: 9 × 4 píxeles.</summary>
    public const int BowlWidth = 9;
    public const int BowlHeight = 4;

    /// <summary>Popó: 5 × 4 píxeles.</summary>
    public const int PoopWidth = 5;
    public const int PoopHeight = 4;

    /// <summary>
    /// Centro de la cabeza (en píxeles desde el borde izquierdo del dibujo, mirando a la derecha)
    /// en las posturas de comer: ahí tiene que quedar el cuenco.
    /// </summary>
    public const double EatingHeadCenter = 17;

    // Fila vacía. Va antes que los dibujos que la usan: los campos se crean en el orden en que se escriben.
    private static readonly string Empty = new('.', Width);

    // Gato: o contorno, b pelo, s rayas, l claro (hocico, panza), e ojo, p pupila, n nariz y lengua, z "zzz".
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

    // Dormido: hecho una bola, con los ojos cerrados.
    private static readonly string[] CatSleep =
    [
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        ".............o...o..",
        "............obo.obo.",
        "...oooooooooobbbbbo.",
        "..obsbbsbbsbooobooo.",
        "..obbbbbbbbbobbnbbo.",
        "..obbbbbbbbbbblllbo.",
        "..oooooooooooolloo..",
    ];

    // Estirándose: patas de delante estiradas, la cola arriba.
    private static readonly string[] CatStretch =
    [
        "....................",
        "....................",
        "....................",
        "....................",
        ".o..................",
        "obo.................",
        ".obooooooo..........",
        "..obsbbsbbo...o...o.",
        "..obbsbbsbbo.obo.obo",
        "..obbbbbbbbbbobbbbbo",
        "..obbbbbbbbbbooobooo",
        "..obbo.obbo..obbnbbo",
        "..obo..obo..ollllllo",
        "..olo..olo..oooooooo",
    ];

    // Beagle: o contorno, w blanco, t canela, k manto negro, r oreja, e ojo, n nariz, q lengua, z "zzz".
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

    // Dormido: tumbado con la cabeza sobre las patas.
    private static readonly string[] DogSleep =
    [
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
        ".............oooo...",
        "...oooooooooottttoo.",
        "..otkkkkkkkkorrtowwo",
        "..ottkkkkkkkorrtwwwn",
        "..otttttttttorrwwwo.",
        ".owwwwwwwwwwwwwwwwo.",
        "..oooooooooooooooo..",
    ];

    // La "reverencia" de juego: patas de delante estiradas y la cola arriba.
    private static readonly string[] DogStretch =
    [
        "....................",
        "....................",
        "....................",
        "....................",
        "..o.................",
        ".owo........oooo....",
        ".oto.......otttto...",
        "..otooooo..ottetoo..",
        "...okkkkkkoorrttwwo.",
        "...ottkkkkkorrtwwwno",
        "...ottttttoorrwwwwo.",
        "...owo.owo.owwwwwwo.",
        "...owo.owo.oooooooo.",
        "...owo.owo..........",
    ];

    // Para comer: el cuerpo de pie sin la cabeza (se pone abajo, delante) y con la cola arriba.
    private static readonly string[] CatEatBody =
    [
        Empty, Empty, Empty, Empty,
        ".o..................",
        "obo.................",
        ".obooooooooooooooo..",
        .. CatBody[1..],
        .. Legs('b', 'l', apart: true),
    ];

    // La cabeza del gato sola (columnas 12 a 18 del dibujo de pie).
    private static readonly string[] CatHeadBlock = [.. CatHead.Select(row => row[12..19])];

    // Beagle comiendo: el cuello baja y el hocico llega al suelo (en B mastica y asoma la lengua).
    private static readonly string[] DogEat =
    [
        "..o.................",
        ".owo................",
        ".oto................",
        "..oto...............",
        "...ot...............",
        "....oooooooooooo....",
        "...otkkkkkkkkkkko...",
        "...ottkkkkkkkkkttoo.",
        "...otttkkkkkktttrrto",
        "...ottttttttttttrrto",
        "...owwwwwwwwwwwrrteo",
        "...owo.owo...oworwwo",
        "...owo.owo...owowwwo",
        "...owo.owo...owoowno",
    ];

    // Una "Z" pequeña y otra grande, para el ir y venir del ronquido.
    private static readonly (int Row, string Text)[] SmallZ =
        [(4, "..................zz"), (5, "...................z"), (6, "..................zz")];
    private static readonly (int Row, string Text)[] BigZ =
        [(3, "................zzzz"), (4, "..................z."), (5, ".................z.."), (6, "................zzzz")];

    // Juguetes de 5×5: o contorno; ovillo y lana, k hebra; pelota r rojo, w blanco; ratón g gris, k ojo, t cola.
    private static readonly Dictionary<PetToy, string[][]> ToyFrames = new()
    {
        [PetToy.Yarn] =
        [
            [".ooo.", "oykyo", "okyko", "oykyo", ".ooo."],
            [".ooo.", "okyyo", "oykyo", "oyyko", ".ooo."],
            [".ooo.", "oyyko", "oykyo", "okyyo", ".ooo."],
        ],
        [PetToy.Ball] =
        [
            [".ooo.", "orrwo", "orwro", "owrro", ".ooo."],
            [".ooo.", "owrro", "orrwo", "orwro", ".ooo."],
            [".ooo.", "orwro", "owrro", "orrwo", ".ooo."],
        ],
        [PetToy.Mouse] =
        [
            [".....", "...o.", ".oggk", "tgggo", ".o.o."],
            [".....", "...o.", ".oggk", "tgggo", "o.o.."],
        ],
    };

    // Cuenco de 9×4: o contorno, b cuenco, w brillo, f y k croquetas (lleno) o vacío.
    private static readonly string[] BowlFull = ["..fkfkf..", ".okfkfko.", "obwbbbbbo", ".ooooooo."];
    private static readonly string[] BowlEmpty = [".........", ".o.....o.", "obwbbbbbo", ".ooooooo."];

    // Popó de 5×4: o contorno, p marrón, h brillo.
    private static readonly string[] PoopRows = ["..o..", ".oho.", "oppho", "ooooo"];

    /// <summary>El cuenco con comida o ya vacío.</summary>
    public static IReadOnlyList<string> Bowl(bool full) => full ? BowlFull : BowlEmpty;

    public static IReadOnlyList<string> Poop() => PoopRows;

    public static IReadOnlyDictionary<char, uint> BowlPalette { get; } = new Dictionary<char, uint>
    {
        ['o'] = 0xFF1E2F4D, ['b'] = 0xFF4F7FD0, ['w'] = 0xFFB9D2F5, ['f'] = 0xFFB0712E, ['k'] = 0xFF7A4518,
    };

    public static IReadOnlyDictionary<char, uint> PoopPalette { get; } = new Dictionary<char, uint>
    {
        ['o'] = 0xFF3A2410, ['p'] = 0xFF7B4A22, ['h'] = 0xFFA8733F,
    };

    public static int ToyFrameCount(PetToy toy) => ToyFrames[toy].Length;

    /// <summary>Imagen del juguete; el número de imagen da la vuelta solo (también si es negativo).</summary>
    public static IReadOnlyList<string> Toy(PetToy toy, int frame)
    {
        string[][] frames = ToyFrames[toy];
        return frames[((frame % frames.Length) + frames.Length) % frames.Length];
    }

    /// <summary>Las filas del dibujo de esa mascota en esa postura.</summary>
    public static IReadOnlyList<string> Frame(PetKind kind, PetPose pose) =>
        kind == PetKind.Beagle ? DogFrame(pose) : CatFrame(pose);

    private static string[] CatFrame(PetPose pose) => pose switch
    {
        PetPose.Sit => CatSit,
        PetPose.SitBlink => Replace(CatSit, (3, "............ooobooo.")),
        PetPose.StandA => [.. CatHead, .. CatBody, .. Legs('b', 'l', apart: true)],
        PetPose.StandB => [.. CatHead, .. CatBody, .. Legs('b', 'l', apart: false)],
        // Agazapado: el cuerpo baja dos píxeles y las patas se doblan.
        PetPose.Crouch => [Empty, Empty, .. CatHead, .. CatBody, "...olo.olo...olo.olo"],
        PetPose.Jump => [.. CatHead, .. CatBody, .. JumpLegs('b')],
        PetPose.Swipe => [.. CatHead, .. CatBody[..3], "...obbbbbbbbbbllbool", CatBody[4], .. SwipeLegs('b', 'l')],
        PetPose.SleepA => Replace(CatSleep, SmallZ),
        PetPose.SleepB => Replace(CatSleep, BigZ),
        PetPose.Stretch => CatStretch,
        // Lamiéndose la pata: ojos cerrados, la pata junto a la boca y la lengua fuera (o dentro).
        PetPose.LickA => Replace(CatSit, (3, "............ooobooo."), (4, "............obbnbbol"),
            (5, "............oblnlbol"), (6, "...........obbbbbbol")),
        // Maullando: la boca abierta (oscura, con la lengua) bajo la nariz.
        PetPose.Talk => Replace(CatSit, (4, "............obbnbbo."), (5, "............olpnplo."), (6, "...........obbppbbo.")),
        // Comiendo: la cabeza baja hasta el cuenco (ojos cerrados) y sube un poco al masticar.
        PetPose.EatA => Overlay(CatEatBody, 9, 13, CatHeadBlock[0], CatHeadBlock[1], CatHeadBlock[2], "ooobooo", CatHeadBlock[4]),
        PetPose.EatB => Overlay(CatEatBody, 8, 13, CatHeadBlock),
        _ => Replace(CatSit, (3, "............ooobooo."), (4, "............obbnbbol"),
            (5, "............oblllbol"), (6, "...........obbbbbbol")),
    };

    private static string[] DogFrame(PetPose pose) => pose switch
    {
        PetPose.Sit => DogSit,
        PetPose.SitBlink => Replace(DogSit, (2, "...........ottotoo..")),
        PetPose.StandA => [.. DogHead, .. DogBody, .. Legs('w', 'w', apart: true)],
        PetPose.StandB => [.. DogHead, .. DogBody, .. Legs('w', 'w', apart: false)],
        PetPose.Crouch => [Empty, Empty, .. DogHead, .. DogBody, "...owo.owo...owo.owo"],
        PetPose.Jump => [.. DogHead, .. DogBody, .. JumpLegs('w')],
        PetPose.Swipe => [.. DogHead, .. DogBody[..4], "...otttttttttttwwoow", DogBody[5], .. SwipeLegs('w', 'w')],
        PetPose.SleepA => Replace(DogSleep, SmallZ),
        PetPose.SleepB => Replace(DogSleep, BigZ),
        PetPose.Stretch => DogStretch,
        // Jadeando contento: ojo cerrado y la lengua fuera (o dentro).
        PetPose.LickA => Replace(DogSit, (2, "...........ottotoo.."), (5, "...........orrwwwwoq")),
        // Ladrando: la mandíbula de abajo baja y se ve la boca.
        PetPose.Talk => Replace(DogSit, (4, "...........orrtwwwwo"), (5, "...........orrwwokqo"), (6, "..........okrrwwwoo.")),
        PetPose.EatA => DogEat,
        PetPose.EatB => Replace(DogEat, (8, "...otttkkkkkktttrro."), (10, "...owwwwwwwwwwwrrtto"),
            (11, "...owo.owo...owrrteo"), (13, "...owo.owo...owoqwno")),
        _ => Replace(DogSit, (2, "...........ottotoo..")),
    };

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

    // Pone el dibujo <layer> encima de <rows> desde la fila <top> y la columna <left> ('.' deja ver lo de abajo).
    private static string[] Overlay(string[] rows, int top, int left, params string[] layer)
    {
        string[] copy = [.. rows];
        for (int y = 0; y < layer.Length && top + y < copy.Length; y++)
        {
            char[] row = copy[top + y].ToCharArray();
            for (int x = 0; x < layer[y].Length && left + x < row.Length; x++)
                if (layer[y][x] != '.')
                    row[left + x] = layer[y][x];
            copy[top + y] = new string(row);
        }
        return copy;
    }

    private static string[] Replace(string[] rows, params (int Row, string Text)[] changes)
    {
        string[] copy = [.. rows];
        foreach ((int row, string text) in changes)
            copy[row] = text;
        return copy;
    }

    /// <summary>Colores (ARGB) de cada letra del dibujo.</summary>
    public static IReadOnlyDictionary<char, uint> Palette(PetKind kind) => kind switch
    {
        PetKind.GrayCat => new Dictionary<char, uint>
        {
            // Gris oscuro, pero no tanto que desaparezca sobre el notch negro.
            ['o'] = 0xFF1C1C21, ['b'] = 0xFF5A5A65, ['s'] = 0xFF474751, ['l'] = 0xFF888894,
            ['e'] = 0xFFF7C51E, ['p'] = 0xFF101010, ['n'] = 0xFFE597AC, ['z'] = ZColor,
        },
        PetKind.TabbyCat => new Dictionary<char, uint>
        {
            ['o'] = 0xFF3D2410, ['b'] = 0xFFF2B33D, ['s'] = 0xFFC2721A, ['l'] = 0xFFFBE4AA,
            ['e'] = 0xFF7CC243, ['p'] = 0xFF101010, ['n'] = 0xFFE597AC, ['z'] = ZColor,
        },
        _ => new Dictionary<char, uint>
        {
            ['o'] = 0xFF2A1A10, ['w'] = 0xFFF4EFE6, ['t'] = 0xFFC98B4A, ['k'] = 0xFF2B2B2B,
            ['r'] = 0xFF7A4A26, ['e'] = 0xFF2E1B10, ['n'] = 0xFF111111, ['q'] = 0xFFE8768E, ['z'] = ZColor,
        },
    };

    // Azul pálido: se ve sobre el notch negro y sobre el blanco.
    private const uint ZColor = 0xFF8FB4E8;

    public static IReadOnlyDictionary<char, uint> ToyPalette(PetToy toy) => toy switch
    {
        PetToy.Yarn => new Dictionary<char, uint> { ['o'] = 0xFF6B2D3F, ['y'] = 0xFFE0607E, ['k'] = 0xFFB03A57 },
        PetToy.Ball => new Dictionary<char, uint> { ['o'] = 0xFF5A1414, ['r'] = 0xFFE23B3B, ['w'] = 0xFFF5F5F5 },
        _ => new Dictionary<char, uint> { ['o'] = 0xFF3A3A40, ['g'] = 0xFFA9A9B4, ['k'] = 0xFF101010, ['t'] = 0xFFE597AC },
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
