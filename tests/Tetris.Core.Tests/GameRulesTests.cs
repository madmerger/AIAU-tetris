using Tetris.Core;

namespace Tetris.Core.Tests;

public class GameRulesTests
{
    [Theory]
    [InlineData(1, 1, 40)]
    [InlineData(2, 1, 100)]
    [InlineData(3, 1, 300)]
    [InlineData(4, 1, 1200)]
    [InlineData(1, 3, 120)]
    [InlineData(4, 5, 6000)]
    public void LineClearScoreMultipliesByLevel(int lines, int level, int expected)
        => Assert.Equal(expected, GameRules.LineClearScore(lines, level));

    [Fact]
    public void LineClearScoreRejectsInvalidLineCount()
        => Assert.Throws<ArgumentOutOfRangeException>(() => GameRules.LineClearScore(5, 1));

    [Fact]
    public void FallIntervalStartsAt800msAndShrinks10PercentPerLevel()
    {
        Assert.Equal(0.8, GameRules.FallInterval(1), 6);
        Assert.Equal(0.72, GameRules.FallInterval(2), 6);
        Assert.Equal(0.648, GameRules.FallInterval(3), 6);
    }

    [Fact]
    public void FallIntervalIsClampedTo50ms()
        => Assert.Equal(0.05, GameRules.FallInterval(100), 6);

    [Theory]
    [InlineData(0, 1)]
    [InlineData(19, 1)]
    [InlineData(20, 2)]
    [InlineData(41, 3)]
    public void LevelIncreasesEvery20Lines(int totalLines, int expected)
        => Assert.Equal(expected, GameRules.LevelForLines(totalLines));
}

public class PieceGeneratorTests
{
    [Fact]
    public void BagContainsEachPieceOncePerCycle()
    {
        var generator = new BagPieceGenerator(new Random(1234));

        for (var cycle = 0; cycle < 20; cycle++)
        {
            var drawn = Enumerable.Range(0, 7).Select(_ => generator.Next()).ToList();
            Assert.Equal(7, drawn.Distinct().Count());
        }
    }

    [Fact]
    public void FixedGeneratorRepeatsSequence()
    {
        var generator = new FixedPieceGenerator(PieceType.I, PieceType.O);

        Assert.Equal(PieceType.I, generator.Next());
        Assert.Equal(PieceType.O, generator.Next());
        Assert.Equal(PieceType.I, generator.Next());
    }
}
