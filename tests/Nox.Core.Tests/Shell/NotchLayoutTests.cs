using Nox.Core.Shell;

namespace Nox.Core.Tests.Shell;

public sealed class NotchLayoutTests
{
    // Pantalla de 1920 × 1080 con la barra de tareas abajo (48 px).
    private static readonly PixelRect Area = new(0, 0, 1920, 1032);

    [Theory]
    [InlineData(0.2, false)]
    [InlineData(0.5, false)]
    [InlineData(0.51, true)]
    [InlineData(0.9, true)]
    public void LowerHalf_GrowsUp(double y, bool expected) => Assert.Equal(expected, NotchLayout.GrowsUp(y));

    [Fact]
    public void ToFraction_AndBack_GivesTheSamePoint()
    {
        var (fx, fy) = NotchLayout.ToFraction(Area, 480, 516);

        Assert.Equal(0.25, fx, 6);
        Assert.Equal(0.5, fy, 6);
        Assert.Equal((480, 516), NotchLayout.PillCenter(Area, fx, fy, 220, 34));
    }

    [Fact]
    public void ToFraction_OutsideTheArea_IsClamped()
    {
        var (fx, fy) = NotchLayout.ToFraction(Area, -50, 5000);

        Assert.Equal((0.0, 1.0), (fx, fy));
    }

    [Fact]
    public void PillCenter_KeepsTheClosedPillOnScreen()
    {
        Assert.Equal((110, 17), NotchLayout.PillCenter(Area, 0, 0, 220, 34));
        Assert.Equal((1810, 1015), NotchLayout.PillCenter(Area, 1, 1, 220, 34));
    }

    [Fact]
    public void PillCenter_FollowsTheResolution()
    {
        var big = new PixelRect(0, 0, 3840, 2064);

        Assert.Equal((960, 516), NotchLayout.PillCenter(big, 0.25, 0.25, 220, 34));
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 8)]
    [InlineData(true, 0)]
    [InlineData(true, 8)]
    public void WindowForPill_PutsThePillWhereWanted(bool growsUp, int gap)
    {
        var (left, top) = NotchLayout.WindowForPill(700, 300, 560, 248, 34, gap, growsUp);

        PixelRect pill = NotchLayout.PillRect(left, top, 560, 248, 220, 34, gap, growsUp);
        Assert.Equal(700, pill.Left + pill.Width / 2);
        Assert.Equal(300, pill.Top + pill.Height / 2);
    }

    [Fact]
    public void PillRect_GrowingUp_HangsFromTheBottomOfTheWindow()
    {
        PixelRect pill = NotchLayout.PillRect(100, 200, 560, 248, 560, 236, 8, growsUp: true);

        Assert.Equal(new PixelRect(100, 204, 660, 440), pill);
    }

    [Fact]
    public void ShiftToFit_ZeroWhenInside() =>
        Assert.Equal((0, 0), NotchLayout.ShiftToFit(Area, new PixelRect(10, 10, 570, 246)));

    [Fact]
    public void ShiftToFit_PushesBackFromEachEdge()
    {
        Assert.Equal((170, 0), NotchLayout.ShiftToFit(Area, new PixelRect(-170, 0, 390, 236)));
        Assert.Equal((-60, 0), NotchLayout.ShiftToFit(Area, new PixelRect(1420, 0, 1980, 236)));
        Assert.Equal((0, -40), NotchLayout.ShiftToFit(Area, new PixelRect(0, 836, 560, 1072)));
        Assert.Equal((0, 25), NotchLayout.ShiftToFit(Area, new PixelRect(0, -25, 560, 211)));
    }

    [Fact]
    public void ShiftToFit_BiggerThanTheArea_AlignsTopLeft() =>
        Assert.Equal((-10, -10), NotchLayout.ShiftToFit(new PixelRect(0, 0, 100, 100), new PixelRect(10, 10, 300, 300)));

    [Fact]
    public void AfterDrag_UpperHalf_ClosedPillSharesTheTopEdge()
    {
        var expanded = new PixelRect(400, 100, 960, 336);

        var (x, y, growsUp) = NotchLayout.PillCenterAfterDrag(Area, expanded, 34);

        Assert.False(growsUp);
        Assert.Equal(680, x);
        Assert.Equal(117, y); // 100 + 34/2
    }

    [Fact]
    public void AfterDrag_LowerHalf_ClosedPillSharesTheBottomEdge()
    {
        var expanded = new PixelRect(400, 700, 960, 936);

        var (x, y, growsUp) = NotchLayout.PillCenterAfterDrag(Area, expanded, 34);

        Assert.True(growsUp);
        Assert.Equal(680, x);
        Assert.Equal(919, y); // 936 - 34/2
    }

    [Fact]
    public void AfterDrag_TheOpenPillDoesNotMove()
    {
        // Se suelta abierto en la mitad de abajo: la ventana se recoloca para crecer hacia arriba
        // y la pastilla abierta tiene que seguir exactamente donde estaba.
        var expanded = new PixelRect(400, 700, 960, 936);
        var (x, y, growsUp) = NotchLayout.PillCenterAfterDrag(Area, expanded, 34);

        var (left, top) = NotchLayout.WindowForPill(x, y, 560, 248, 34, 8, growsUp);

        Assert.Equal(expanded, NotchLayout.PillRect(left, top, 560, 248, 560, 236, 8, growsUp));
    }
}
