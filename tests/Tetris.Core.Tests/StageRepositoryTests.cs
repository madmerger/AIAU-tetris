using System.Linq;
using Tetris.Core;

namespace Tetris.Core.Tests;

public class StageRepositoryTests
{
    private static string EmptyRow => string.Join(",", Enumerable.Repeat("0", Board.Width));

    private static string BuildStage(params string[] bottomRows)
    {
        var rows = Enumerable.Repeat(EmptyRow, Board.Height - bottomRows.Length).Concat(bottomRows);
        return string.Join("\n", new[] { "# stage 1" }.Concat(rows));
    }

    [Fact]
    public void EmbeddedDataContainsTwentyValidStages()
    {
        var stages = StageRepository.Stages;

        Assert.Equal(20, stages.Count);
        for (int i = 0; i < stages.Count; i++)
        {
            var stage = stages[i];
            Assert.Equal(i + 1, stage.Number);
            Assert.True(stage.GemCount > 0, $"ステージ {stage.Number} にジェムがありません");

            var board = new Board();
            board.LoadLayout(stage);
            Assert.Empty(board.FindFullRows());
            Assert.True(board.CanPlace(TetrisGame.CreateSpawnPiece(PieceType.O)), "出現位置が初期状態で埋まっています");
        }
    }

    [Fact]
    public void GemCountIncreasesWithStageNumber()
    {
        var stages = StageRepository.Stages;
        Assert.True(stages[^1].GemCount >= stages[0].GemCount);
    }

    [Fact]
    public void ParseAcceptsRowsWithoutCommas()
    {
        var stages = StageRepository.Parse(BuildStage("2111111100"));

        Assert.Single(stages);
        Assert.Equal(CellKind.Gem, stages[0][0, Board.Height - 1].Kind);
        Assert.Equal(CellKind.Wall, stages[0][1, Board.Height - 1].Kind);
        Assert.True(stages[0][9, Board.Height - 1].IsEmpty);
    }

    [Fact]
    public void ParseRejectsWrongRowCount()
    {
        var text = string.Join("\n", new[] { "# stage 1" }.Concat(Enumerable.Repeat(EmptyRow, Board.Height - 1)));
        var error = Assert.Throws<StageDataException>(() => StageRepository.Parse(text));
        Assert.Contains("行数", error.Message);
    }

    [Fact]
    public void ParseRejectsWrongColumnCount()
    {
        var error = Assert.Throws<StageDataException>(() => StageRepository.Parse(BuildStage("2,1,1")));
        Assert.Contains("列数", error.Message);
    }

    [Fact]
    public void ParseRejectsUnknownCharacters()
    {
        var error = Assert.Throws<StageDataException>(() => StageRepository.Parse(BuildStage("2,1,1,1,1,1,1,1,0,X")));
        Assert.Contains("未知の文字", error.Message);
    }

    [Fact]
    public void ParseRejectsStageWithoutGems()
    {
        var error = Assert.Throws<StageDataException>(() => StageRepository.Parse(BuildStage("1,1,1,1,1,1,1,1,0,0")));
        Assert.Contains("ジェム", error.Message);
    }

    [Fact]
    public void ParseRejectsDataWithoutStageHeader()
    {
        var error = Assert.Throws<StageDataException>(() => StageRepository.Parse(EmptyRow));
        Assert.Contains("見出し", error.Message);
    }

    [Fact]
    public void ParseRejectsNonSequentialStageNumbers()
    {
        var text = BuildStage("2,1,1,1,1,1,1,1,0,0").Replace("# stage 1", "# stage 3");
        var error = Assert.Throws<StageDataException>(() => StageRepository.Parse(text));
        Assert.Contains("連番", error.Message);
    }
}
