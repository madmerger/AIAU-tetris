using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tetris.Core;

namespace Tetris.Core.Tests;

public class StageModeTests
{
    /// <summary>最下行のみ「x=0 がジェム、x=8,9 が空、他は壁」の面を指定数だけ作る。</summary>
    private static IReadOnlyList<StageLayout> BuildTestStages(int count)
    {
        var text = new StringBuilder();
        for (int i = 1; i <= count; i++)
        {
            text.AppendLine($"# stage {i}");
            for (int y = 0; y < Board.Height - 1; y++)
            {
                text.AppendLine(string.Join(",", Enumerable.Repeat("0", Board.Width)));
            }

            text.AppendLine("2,1,1,1,1,1,1,1,0,0");
        }

        return StageRepository.Parse(text.ToString());
    }

    private static TetrisGame NewStageGame(int stageCount = 2) =>
        new(GameMode.Stage, seed: 1, stages: BuildTestStages(stageCount), pieceSource: new FixedPieceSource(PieceType.O));

    /// <summary>右端 2 列に O ピースを落として最下行を消す。</summary>
    private static void ClearBottomRow(TetrisGame game)
    {
        while (game.MoveRight())
        {
        }

        game.HardDrop();
        game.Update(TimeSpan.FromSeconds(0.31));
    }

    [Fact]
    public void StageModeLoadsInitialLayout()
    {
        var game = NewStageGame();

        Assert.Equal(1, game.StageNumber);
        Assert.Equal(2, game.StageCount);
        Assert.Equal(CellKind.Gem, game.Board[0, Board.Height - 1].Kind);
        Assert.Equal(CellKind.Wall, game.Board[1, Board.Height - 1].Kind);
        Assert.True(game.Board[8, Board.Height - 1].IsEmpty);
        Assert.Equal(1, game.Board.CountGems());
    }

    [Fact]
    public void ClearingAllGemsTriggersStageClearCountdownThenNextStage()
    {
        var game = NewStageGame();
        game.DrainSounds();

        ClearBottomRow(game);

        Assert.Equal(GamePhase.StageClear, game.Phase);
        Assert.Equal(0, game.Board.CountGems());
        Assert.Contains(GameSound.StageClear, game.DrainSounds());
        Assert.Equal(3, game.CountdownSeconds);

        game.Update(TimeSpan.FromSeconds(1.2));
        Assert.Equal(GamePhase.StageClear, game.Phase);
        Assert.Equal(2, game.CountdownSeconds);

        game.Update(TimeSpan.FromSeconds(2));
        Assert.Equal(GamePhase.Playing, game.Phase);
        Assert.Equal(2, game.StageNumber);
        Assert.Equal(1, game.Board.CountGems());
    }

    [Fact]
    public void ScoreAndLevelAreNotCarriedOverToNextStage()
    {
        var game = NewStageGame();
        ClearBottomRow(game);
        Assert.True(game.Score > 0);

        game.Update(TimeSpan.FromSeconds(3));

        Assert.Equal(2, game.StageNumber);
        Assert.Equal(0, game.Score);
        Assert.Equal(1, game.Level);
        Assert.Equal(0, game.LinesCleared);
        Assert.Equal(TimeSpan.Zero, game.Elapsed);
    }

    [Fact]
    public void ClearingTheFinalStageResultsInAllClear()
    {
        var game = NewStageGame(stageCount: 2);

        ClearBottomRow(game);
        game.Update(TimeSpan.FromSeconds(3));
        Assert.Equal(2, game.StageNumber);

        ClearBottomRow(game);
        Assert.Equal(GamePhase.StageClear, game.Phase);
        game.Update(TimeSpan.FromSeconds(3));

        Assert.Equal(GamePhase.AllClear, game.Phase);
        Assert.Null(game.Current);
        game.Update(TimeSpan.FromSeconds(10));
        Assert.Equal(GamePhase.AllClear, game.Phase);
    }

    [Fact]
    public void StageIsNotClearedWhileGemsRemain()
    {
        var stages = StageRepository.Parse(
            string.Join(
                "\n",
                new[] { "# stage 1" }
                    .Concat(Enumerable.Repeat(string.Join(",", Enumerable.Repeat("0", Board.Width)), Board.Height - 2))
                    .Concat(new[] { "2,0,0,0,0,0,0,0,0,0", "2,1,1,1,1,1,1,1,0,0" })));
        var game = new TetrisGame(GameMode.Stage, seed: 1, stages: stages, pieceSource: new FixedPieceSource(PieceType.O));

        Assert.Equal(2, game.Board.CountGems());
        ClearBottomRow(game);

        Assert.Equal(GamePhase.Playing, game.Phase);
        Assert.Equal(1, game.Board.CountGems());
        Assert.Equal(1, game.StageNumber);
    }

    [Fact]
    public void RestartReloadsTheCurrentStage()
    {
        var game = NewStageGame();
        ClearBottomRow(game);
        game.Update(TimeSpan.FromSeconds(3));
        Assert.Equal(2, game.StageNumber);

        game.HardDrop();
        game.Restart();

        Assert.Equal(2, game.StageNumber);
        Assert.Equal(GamePhase.Playing, game.Phase);
        Assert.Equal(0, game.Score);
        Assert.Equal(1, game.Board.CountGems());
        Assert.Equal(CellKind.Wall, game.Board[1, Board.Height - 1].Kind);
    }

    [Fact]
    public void StageModeRequiresStageData()
    {
        Assert.Throws<StageDataException>(() =>
            new TetrisGame(GameMode.Stage, stages: Array.Empty<StageLayout>()));
    }
}
