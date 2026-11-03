using Nox.Core.Pets;

namespace Nox.Core.Tests.Pets;

public sealed class PetTalkAndFoodTests
{
    [Fact]
    public void LongPlay_SometimesTalks_WithTheBubble()
    {
        var bubbles = new HashSet<PetBubble>();
        int talks = 0;
        foreach (int seed in new[] { 1, 2, 3 })
        {
            var playground = new PetPlayground(seed);
            for (int i = 0; i < 20 * 300; i++) // 5 minutos
            {
                playground.Step(0.05);
                bubbles.Add(playground.Bubble);
                if (playground.Pose == PetPose.Talk)
                    Assert.Equal(PetBubble.Talk, playground.Bubble);
            }
            talks += playground.Talks;
        }

        Assert.True(talks > 0, "nunca maulló");
        Assert.Contains(PetBubble.Talk, bubbles);
        Assert.DoesNotContain(PetBubble.Hungry, bubbles);
    }

    [Fact]
    public void Touch_MakesItTalk_FacingTheMouse()
    {
        var playground = new PetPlayground(6);
        playground.Step(0.05);

        playground.Touch(0);
        playground.Step(0.05);

        Assert.True(playground.IsTalking);
        Assert.Equal(PetPose.Talk, playground.Pose);
        Assert.Equal(PetBubble.Talk, playground.Bubble);
        Assert.False(playground.FacingRight);
    }

    [Fact]
    public void Talk_EndsByItself()
    {
        var playground = new PetPlayground(6);
        playground.Touch(0);

        playground.Step(2);

        Assert.False(playground.IsTalking);
    }

    [Fact]
    public void Hungry_ShowsTheHungryBubble_AndNeverSleeps()
    {
        var playground = new PetPlayground(2) { IsHungry = true };
        for (int i = 0; i < 20 * 600; i++) // 10 minutos
        {
            playground.Step(0.05);
            Assert.False(playground.IsSleeping);
            Assert.Equal(PetBubble.Hungry, playground.Bubble);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(11)]
    public void PlaceFood_PetWalksToTheBowl_AndEatsWithItsHeadOverIt(int seed)
    {
        var playground = new PetPlayground(seed, stageWidth: 128) { IsHungry = true };
        playground.Step(1);

        playground.PlaceFood();

        Assert.True(playground.HasBowl && playground.BowlFull);
        bool ate = false;
        for (int i = 0; i < 20 * 10 && !ate; i++)
        {
            playground.Step(0.05);
            if (playground.IsEating)
            {
                ate = true;
                Assert.Contains(playground.Pose, new[] { PetPose.EatA, PetPose.EatB });
                Assert.Equal(PetBubble.None, playground.Bubble); // comiendo no se queja
                double head = playground.FacingRight
                    ? playground.PetX + PetSprites.EatingHeadCenter
                    : playground.PetX + PetSprites.Width - PetSprites.EatingHeadCenter;
                Assert.InRange(head, playground.BowlX, playground.BowlX + PetSprites.BowlWidth);
            }
        }
        Assert.True(ate, "no llegó a comer");
    }

    [Fact]
    public void AfterEating_BowlIsEmpty_ThenDisappears()
    {
        var playground = new PetPlayground(3);
        playground.PlaceFood();
        for (int i = 0; i < 20 * 20 && (playground.BowlFull || playground.IsEating); i++)
            playground.Step(0.05);

        Assert.True(playground.HasBowl);
        Assert.False(playground.BowlFull);

        playground.Step(3.5);
        Assert.False(playground.HasBowl);
    }

    [Fact]
    public void Eating_IgnoresTheMouse()
    {
        var playground = new PetPlayground(3);
        playground.PlaceFood();
        for (int i = 0; i < 20 * 10 && !playground.IsEating; i++)
            playground.Step(0.05);
        Assert.True(playground.IsEating);

        playground.Step(2.5);
        playground.Touch(0);
        playground.Poke(0);
        playground.Step(0.05);

        Assert.True(playground.IsEating);
    }

    [Fact]
    public void Poops_AreClamped_AndSpreadOverTheFloor()
    {
        var playground = new PetPlayground(1) { Poops = 7 };
        Assert.Equal(PetCare.MaxPoops, playground.Poops);

        double[] spots = [playground.PoopX(0), playground.PoopX(1), playground.PoopX(2)];
        Assert.All(spots, x => Assert.InRange(x, 0, playground.StageWidth - PetSprites.PoopWidth));
        Assert.True(spots[1] - spots[0] > PetSprites.PoopWidth && spots[2] - spots[1] > PetSprites.PoopWidth);

        playground.Poops = -1;
        Assert.Equal(0, playground.Poops);
    }

    [Fact]
    public void BowlAndPoop_SpritesUseTheirPalettes()
    {
        foreach (bool full in new[] { true, false })
        {
            IReadOnlyList<string> bowl = PetSprites.Bowl(full);
            Assert.Equal(PetSprites.BowlHeight, bowl.Count);
            Assert.All(bowl, row => Assert.Equal(PetSprites.BowlWidth, row.Length));
            PetSprites.Render(bowl, PetSprites.BowlPalette);
        }
        IReadOnlyList<string> poop = PetSprites.Poop();
        Assert.Equal(PetSprites.PoopHeight, poop.Count);
        Assert.All(poop, row => Assert.Equal(PetSprites.PoopWidth, row.Length));
        PetSprites.Render(poop, PetSprites.PoopPalette);
    }

    [Fact]
    public void FullAndEmptyBowl_LookDifferent() =>
        Assert.NotEqual(PetSprites.Bowl(true), PetSprites.Bowl(false));

    [Theory]
    [InlineData(PetKind.GrayCat, PetBubble.Talk, "¡Miau!")]
    [InlineData(PetKind.TabbyCat, PetBubble.Talk, "¡Miau!")]
    [InlineData(PetKind.Beagle, PetBubble.Talk, "¡Guau!")]
    [InlineData(PetKind.Beagle, PetBubble.Hungry, "¡Tengo hambre!")]
    [InlineData(PetKind.GrayCat, PetBubble.None, "")]
    public void Speech_DependsOnThePet(PetKind kind, PetBubble bubble, string expected) =>
        Assert.Equal(expected, PetSpeech.Text(kind, bubble));

    [Theory]
    [InlineData(PetKind.GrayCat)]
    [InlineData(PetKind.Beagle)]
    public void TalkAndEatPoses_AreDifferentFromSitting(PetKind kind)
    {
        Assert.NotEqual(PetSprites.Frame(kind, PetPose.Sit), PetSprites.Frame(kind, PetPose.Talk));
        Assert.NotEqual(PetSprites.Frame(kind, PetPose.EatA), PetSprites.Frame(kind, PetPose.EatB));
    }
}
