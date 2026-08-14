using Tetris.Core;

namespace Tetris.Core.Tests;

public class StageModeTests
{
    /// <summary>最下段が「ジェム + 壁 8 個 + 右端に空き 1 列」のステージ。縦 I を右端に落とすとクリアできる。</summary>
    private static StageData OneGemStage(int number)
        => StageData.Parse(number, BoardTests.StageRows("2111111110"));

    private static TetrisGame NewStageGame(int stageCount)
    {
        var stages = Enumerable.Range(1, stageCount).Select(OneGemStage).ToArray();
        return new TetrisGame(GameMode.Stage, new FixedPieceGenerator(PieceType.I), stages);
    }

    [Fact]
    public void StageModeLoadsInitialLayout()
    {
        var game = NewStageGame(3);

        Assert.Equal(GameMode.Stage, game.Mode);
        Assert.Equal(1, game.StageNumber);
        Assert.Equal(3, game.StageCount);
        Assert.Equal(1, game.RemainingGems);
        Assert.Equal(CellKind.Wall, game.Board[1, 19].Kind);
        Assert.Equal(CellKind.Gem, game.Board[0, 19].Kind);
    }

    [Fact]
    public void StageModeRequiresStageData()
        => Assert.Throws<ArgumentException>(
            () => new TetrisGame(GameMode.Stage, new FixedPieceGenerator(PieceType.I), Array.Empty<StageData>()));

    [Fact]
    public void ClearingAllGemsTriggersStageClearCountdown()
    {
        var game = NewStageGame(3);

        TetrisGameTests.DropVerticalIAt(game, column: 9, finishAnimation: false);
        Assert.Equal(GamePhase.LineClearing, game.Phase);

        game.Update(GameRules.LineClearAnimationSeconds);

        Assert.Equal(GamePhase.StageClear, game.Phase);
        Assert.Equal(0, game.RemainingGems);
        Assert.Equal(3.0, game.StageClearRemaining, 6);
        Assert.Contains(GameEventType.StageCleared, game.DrainEvents());
    }

    [Fact]
    public void NextStageStartsAfterCountdownWithoutCarryingScore()
    {
        var game = NewStageGame(3);

        TetrisGameTests.DropVerticalIAt(game, column: 9);
        Assert.Equal(GamePhase.StageClear, game.Phase);
        Assert.True(game.Score > 0);

        game.Update(2.9);
        Assert.Equal(GamePhase.StageClear, game.Phase);

        game.Update(0.2);
        Assert.Equal(GamePhase.Playing, game.Phase);
        Assert.Equal(2, game.StageNumber);
        Assert.Equal(0, game.Score);
        Assert.Equal(1, game.Level);
        Assert.Equal(0, game.TotalLines);
        Assert.Equal(1, game.RemainingGems);
    }

    [Fact]
    public void AllClearAfterFinalStage()
    {
        var game = NewStageGame(2);

        for (var stage = 1; stage <= 2; stage++)
        {
            Assert.Equal(stage, game.StageNumber);
            TetrisGameTests.DropVerticalIAt(game, column: 9);
            game.Update(GameRules.StageClearCountdownSeconds);
        }

        Assert.Equal(GamePhase.AllClear, game.Phase);
        Assert.Contains(GameEventType.AllCleared, game.DrainEvents());

        game.Update(1.0);
        Assert.Equal(GamePhase.AllClear, game.Phase);
    }

    [Fact]
    public void AllClearFinishesAfterDisplayDuration()
    {
        var game = NewStageGame(1);

        TetrisGameTests.DropVerticalIAt(game, column: 9);
        game.Update(GameRules.StageClearCountdownSeconds);
        Assert.Equal(GamePhase.AllClear, game.Phase);
        Assert.Equal(GameRules.AllClearDisplaySeconds, game.AllClearRemaining);

        game.Update(GameRules.AllClearDisplaySeconds - 0.1);
        Assert.DoesNotContain(GameEventType.AllClearFinished, game.DrainEvents());

        game.Update(0.2);
        Assert.Equal(0, game.AllClearRemaining);
        Assert.Contains(GameEventType.AllClearFinished, game.DrainEvents());

        // 通知は一度だけ。
        game.Update(1.0);
        Assert.DoesNotContain(GameEventType.AllClearFinished, game.DrainEvents());
    }

    [Fact]
    public void InputIsDiscardedDuringStageClearAnimation()
    {
        var game = NewStageGame(2);

        TetrisGameTests.DropVerticalIAt(game, column: 9);

        Assert.Equal(GamePhase.StageClear, game.Phase);
        Assert.False(game.MoveLeft());
        Assert.False(game.RotateClockwise());
        Assert.False(game.HardDrop());
    }

    [Fact]
    public void ClearingLineWithGemsRemainingKeepsPlaying()
    {
        var stage = StageData.Parse(1, BoardTests.StageRows("2000000000", "2111111110"));
        var game = new TetrisGame(GameMode.Stage, new FixedPieceGenerator(PieceType.I), new[] { stage });

        Assert.Equal(2, game.RemainingGems);

        TetrisGameTests.DropVerticalIAt(game, column: 9);

        Assert.Equal(GamePhase.Playing, game.Phase);
        Assert.Equal(1, game.RemainingGems);
    }

    [Fact]
    public void RestartReloadsCurrentStage()
    {
        var game = NewStageGame(3);
        TetrisGameTests.DropVerticalIAt(game, column: 9);
        game.Update(GameRules.StageClearCountdownSeconds);
        Assert.Equal(2, game.StageNumber);

        game.HardDrop();
        game.Restart();

        Assert.Equal(2, game.StageNumber);
        Assert.Equal(0, game.Score);
        Assert.Equal(1, game.RemainingGems);
        Assert.Equal(GamePhase.Playing, game.Phase);
    }

    [Fact]
    public void EmbeddedStagesAreUsedByDefault()
    {
        var game = new TetrisGame(GameMode.Stage, new BagPieceGenerator(new Random(7)));

        Assert.Equal(StageSet.ExpectedStageCount, game.StageCount);
        Assert.True(game.RemainingGems > 0);
        Assert.Equal(GamePhase.Playing, game.Phase);
    }
}
