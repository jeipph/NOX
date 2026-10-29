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

    [Fact]
    public void BallFrames_AreSquare_AndCycle()
    {
        for (int i = 0; i < PetSprites.BallFrameCount; i++)
        {
            IReadOnlyList<string> rows = PetSprites.Ball(i);
            Assert.Equal(PetSprites.BallSize, rows.Count);
            Assert.All(rows, row => Assert.Equal(PetSprites.BallSize, row.Length));
            PetSprites.Render(rows, PetSprites.BallPalette);
        }
        Assert.Equal(PetSprites.Ball(0), PetSprites.Ball(PetSprites.BallFrameCount));
        Assert.Equal(PetSprites.Ball(PetSprites.BallFrameCount - 1), PetSprites.Ball(-1));
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
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(42)]
    public void TwoMinutesOfPlay_StaysOnStage_AndPlaysAndJumps(int seed)
    {
        var playground = new PetPlayground(seed);
        var poses = new HashSet<PetPose>();
        double highest = 0;

        for (int i = 0; i < 20 * 120; i++) // 2 minutos a 20 imágenes por segundo
        {
            playground.Step(0.05);
            poses.Add(playground.Pose);
            highest = Math.Max(highest, playground.PetY);

            Assert.InRange(playground.PetX, 0, PetPlayground.StageWidth - PetSprites.Width);
            Assert.InRange(playground.PetY, 0, PetPlayground.StageHeight - PetSprites.Height);
            Assert.InRange(playground.BallX, 0, PetPlayground.StageWidth - PetSprites.BallSize);
            Assert.True(playground.BallY >= 0);
        }

        Assert.True(playground.Kicks > 0, "nunca tocó el ovillo");
        Assert.Contains(PetPose.Jump, poses);
        Assert.Contains(PetPose.Swipe, poses);
        Assert.Contains(PetPose.StandB, poses); // anduvo
        Assert.True(highest > 8, "los saltos son demasiado bajos");
    }

    [Fact]
    public void BigTimeJump_DoesNotEscapeTheStage()
    {
        var playground = new PetPlayground(3);

        playground.Step(30); // p. ej. el PC estuvo dormido

        Assert.InRange(playground.PetX, 0, PetPlayground.StageWidth - PetSprites.Width);
        Assert.InRange(playground.BallX, 0, PetPlayground.StageWidth - PetSprites.BallSize);
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
        Assert.Equal((a.PetX, a.PetY, a.Pose, a.BallX), (b.PetX, b.PetY, b.Pose, b.BallX));
    }
}
