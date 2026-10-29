using Nox.Core.Pets;

namespace Nox.Core.Tests.Pets;

public sealed class PetTests
{
    public static TheoryData<PetKind, PetPose> AllFrames()
    {
        var data = new TheoryData<PetKind, PetPose>();
        foreach (PetKind kind in Enum.GetValues<PetKind>())
            foreach (PetPose pose in Enum.GetValues<PetPose>())
                data.Add(kind, pose);
        return data;
    }

    [Theory]
    [MemberData(nameof(AllFrames))]
    public void EveryFrame_HasTheSameSize_AndOnlyKnownColors(PetKind kind, PetPose pose)
    {
        IReadOnlyList<string> rows = PetSprites.Frame(kind, pose);

        Assert.Equal(PetSprites.Height, rows.Count);
        Assert.All(rows, row => Assert.Equal(PetSprites.Width, row.Length));
        uint[] pixels = PetSprites.Render(rows, PetSprites.Palette(kind)); // lanza si falta un color
        Assert.Contains(pixels, p => p != 0);
    }

    [Theory]
    [InlineData(PetToy.Yarn)]
    [InlineData(PetToy.Ball)]
    [InlineData(PetToy.Mouse)]
    public void ToyFrames_AreSquare_AndCycle(PetToy toy)
    {
        int count = PetSprites.ToyFrameCount(toy);
        for (int i = 0; i < count; i++)
        {
            IReadOnlyList<string> rows = PetSprites.Toy(toy, i);
            Assert.Equal(PetSprites.ToySize, rows.Count);
            Assert.All(rows, row => Assert.Equal(PetSprites.ToySize, row.Length));
            PetSprites.Render(rows, PetSprites.ToyPalette(toy));
        }
        Assert.Equal(PetSprites.Toy(toy, 0), PetSprites.Toy(toy, count));
        Assert.Equal(PetSprites.Toy(toy, count - 1), PetSprites.Toy(toy, -1));
    }

    [Fact]
    public void Render_DotIsTransparent_LettersUsePalette()
    {
        uint[] pixels = PetSprites.Render(["o.", ".o"], new Dictionary<char, uint> { ['o'] = 0xFF112233 });

        Assert.Equal([0xFF112233u, 0u, 0u, 0xFF112233u], pixels);
    }

    [Fact]
    public void Render_UnknownColor_Throws() =>
        Assert.Throws<ArgumentException>(() => PetSprites.Render(["x"], new Dictionary<char, uint>()));

    [Fact]
    public void GrayCat_HasYellowEyes()
    {
        uint eye = PetSprites.Palette(PetKind.GrayCat)['e'];
        byte red = (byte)(eye >> 16), green = (byte)(eye >> 8), blue = (byte)eye;
        Assert.True(red > 200 && green > 150 && blue < 80, $"El ojo no es amarillo: {eye:X8}");
    }

    [Theory]
    [InlineData(1, PetToy.Yarn)]
    [InlineData(7, PetToy.Ball)]
    [InlineData(42, PetToy.Mouse)]
    public void TwoMinutesOfPlay_StaysOnStage_AndPlaysAndJumps(int seed, PetToy toy)
    {
        var playground = new PetPlayground(seed) { Toy = toy };
        var poses = new HashSet<PetPose>();
        double highest = 0;

        for (int i = 0; i < 20 * 120; i++) // 2 minutos a 20 imágenes por segundo
        {
            playground.Step(0.05);
            poses.Add(playground.Pose);
            highest = Math.Max(highest, playground.PetY);
            AssertInside(playground);
        }

        Assert.True(playground.Kicks > 0, "nunca tocó el juguete");
        Assert.Contains(PetPose.Jump, poses);
        Assert.Contains(PetPose.Swipe, poses);
        Assert.Contains(PetPose.StandB, poses); // anduvo
        Assert.True(highest > 8, "los saltos son demasiado bajos");
    }

    [Fact]
    public void LongPlay_AlsoSleepsStretchesAndGrooms()
    {
        var poses = new HashSet<PetPose>();
        foreach (int seed in new[] { 1, 2, 3 })
        {
            var playground = new PetPlayground(seed);
            for (int i = 0; i < 20 * 600; i++) // 10 minutos
            {
                playground.Step(0.05);
                poses.Add(playground.Pose);
            }
        }

        Assert.Contains(PetPose.SleepA, poses);
        Assert.Contains(PetPose.SleepB, poses);
        Assert.Contains(PetPose.Stretch, poses);
        Assert.Contains(PetPose.LickA, poses);
        Assert.Contains(PetPose.LickB, poses);
    }

    [Fact]
    public void MouseToy_RunsAwayByItself()
    {
        var playground = new PetPlayground(9) { Toy = PetToy.Mouse };
        double start = playground.ToyX;
        bool moved = false;

        // Unos segundos con la mascota quieta al principio: el ratón se escapa sin que nadie lo toque.
        for (int i = 0; i < 20 * 8 && playground.Kicks == 0; i++)
        {
            playground.Step(0.05);
            moved |= Math.Abs(playground.ToyX - start) > 5;
        }

        Assert.True(moved, "el ratón de juguete no se movió solo");
    }

    [Fact]
    public void Poke_WakesASleepingPet()
    {
        var playground = new PetPlayground(2);
        for (int i = 0; i < 20 * 900 && !playground.IsSleeping; i++)
            playground.Step(0.05);
        Assert.True(playground.IsSleeping, "no llegó a dormirse");

        playground.Step(2.5); // pasado el tiempo mínimo entre reacciones
        if (!playground.IsSleeping)
            return; // se despertó solo justo ahora: no hay nada que probar
        playground.Poke(0);
        playground.Step(0.05);

        Assert.False(playground.IsSleeping);
        Assert.False(playground.FacingRight); // mira hacia donde estaba el ratón
    }

    [Fact]
    public void Poke_IsIgnored_RightAfterAnother()
    {
        var playground = new PetPlayground(4);
        playground.Step(3);
        playground.Poke(PetPlayground.DefaultStageWidth); // a la derecha
        bool facing = playground.FacingRight;

        playground.Poke(0); // enseguida, a la izquierda: se ignora

        Assert.Equal(facing, playground.FacingRight);
    }

    [Fact]
    public void WideStage_PetUsesAllOfIt()
    {
        var playground = new PetPlayground(5, stageWidth: 480) { Toy = PetToy.Ball };
        double leftmost = double.MaxValue, rightmost = 0;
        for (int i = 0; i < 20 * 300; i++)
        {
            playground.Step(0.05);
            AssertInside(playground);
            leftmost = Math.Min(leftmost, playground.PetX);
            rightmost = Math.Max(rightmost, playground.PetX);
        }

        Assert.True(rightmost - leftmost > 150, $"apenas se movió: {leftmost:0}–{rightmost:0}");
    }

    [Fact]
    public void BigTimeJump_DoesNotEscapeTheStage()
    {
        var playground = new PetPlayground(3);

        playground.Step(30); // p. ej. el PC estuvo dormido

        AssertInside(playground);
    }

    [Fact]
    public void SameSeed_SamePlay()
    {
        var a = new PetPlayground(5);
        var b = new PetPlayground(5);
        for (int i = 0; i < 400; i++)
        {
            a.Step(0.05);
            b.Step(0.05);
        }
        Assert.Equal((a.PetX, a.PetY, a.Pose, a.ToyX), (b.PetX, b.PetY, b.Pose, b.ToyX));
    }

    private static void AssertInside(PetPlayground p)
    {
        Assert.InRange(p.PetX, 0, p.StageWidth - PetSprites.Width);
        Assert.InRange(p.PetY, 0, PetPlayground.StageHeight - PetSprites.Height);
        Assert.InRange(p.ToyX, 0, p.StageWidth - PetSprites.ToySize);
        Assert.InRange(p.ToyY, 0, PetPlayground.StageHeight - PetSprites.ToySize);
    }
}
