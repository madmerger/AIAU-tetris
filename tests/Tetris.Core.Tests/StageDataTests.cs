using Tetris.Core;

namespace Tetris.Core.Tests;

public class StageDataTests
{
    [Fact]
    public void ParseAcceptsValidLayout()
    {
        var stage = StageData.Parse(1, BoardTests.StageRows("1111211111"));

        Assert.Equal(1, stage.Number);
        Assert.Equal(10, stage.Width);
        Assert.Equal(20, stage.Height);
        Assert.Equal(1, stage.GemCount);
        Assert.Equal(CellKind.Gem, stage[4, 19].Kind);
        Assert.Equal(CellKind.Wall, stage[0, 19].Kind);
        Assert.Equal(CellKind.Empty, stage[0, 18].Kind);
    }

    [Fact]
    public void ParseRejectsWrongRowCount()
    {
        var rows = Enumerable.Repeat("2000000000", 19).ToArray();

        var error = Assert.Throws<StageDataException>(() => StageData.Parse(3, rows));
        Assert.Contains("行数", error.Message);
    }

    [Fact]
    public void ParseRejectsWrongColumnCount()
    {
        var rows = BoardTests.StageRows("2000000000");
        rows[19] = "200000000";

        var error = Assert.Throws<StageDataException>(() => StageData.Parse(4, rows));
        Assert.Contains("列数", error.Message);
    }

    [Fact]
    public void ParseRejectsUnknownCharacter()
    {
        var rows = BoardTests.StageRows("20000000X0");

        var error = Assert.Throws<StageDataException>(() => StageData.Parse(5, rows));
        Assert.Contains("未知の文字", error.Message);
    }

    [Fact]
    public void ParseRejectsLayoutWithoutGem()
        => Assert.Throws<StageDataException>(() => StageData.Parse(6, BoardTests.StageRows("1111111111")));

    [Fact]
    public void EmbeddedStageSetHas20ValidStages()
    {
        var set = StageSet.Embedded;

        Assert.Equal(20, set.Count);
        for (var number = 1; number <= set.Count; number++)
        {
            var stage = set[number];
            Assert.Equal(number, stage.Number);
            Assert.True(stage.GemCount > 0);

            // 出現位置（上部 2 行）は必ず空いていること。
            for (var y = 0; y < 2; y++)
            {
                for (var x = 0; x < stage.Width; x++)
                {
                    Assert.Equal(CellKind.Empty, stage[x, y].Kind);
                }
            }
        }
    }

    [Fact]
    public void EmbeddedStagesLeaveAtLeastOneHolePerRow()
    {
        foreach (var stage in StageSet.Embedded.Stages)
        {
            for (var y = 0; y < stage.Height; y++)
            {
                var holes = Enumerable.Range(0, stage.Width).Count(x => stage[x, y].Kind == CellKind.Empty);
                Assert.True(holes > 0, $"ステージ {stage.Number} の {y + 1} 行目に空きがない。");
            }
        }
    }

    [Fact]
    public void ParseStageSetRejectsUnexpectedStageCount()
    {
        var text = string.Join("\n", BoardTests.StageRows("2000000000"));

        Assert.Throws<StageDataException>(() => StageSet.Parse(text, expectedCount: 20));
    }

    [Fact]
    public void ParseStageSetSplitsStagesOnBlankLinesAndComments()
    {
        var one = string.Join("\n", BoardTests.StageRows("2000000000"));
        var two = string.Join("\n", BoardTests.StageRows("0000000002"));

        var set = StageSet.Parse($"# Stage 1\n{one}\n\n# Stage 2\n{two}\n");

        Assert.Equal(2, set.Count);
        Assert.Equal(CellKind.Gem, set[1][0, 19].Kind);
        Assert.Equal(CellKind.Gem, set[2][9, 19].Kind);
    }
}
