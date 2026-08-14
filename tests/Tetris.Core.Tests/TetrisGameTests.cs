using System;
using System.Linq;
using Tetris.Core;

namespace Tetris.Core.Tests;

public class TetrisGameTests
{
    private static TetrisGame NewInfiniteGame(params PieceType[] pieces) =>
        new(GameMode.Infinite, seed: 1, pieceSource: new FixedPieceSource(pieces.Length == 0 ? new[] { PieceType.O } : pieces));

    [Fact]
    public void NewGameStartsPlayingWithLevelOneAndEmptyBoard()
    {
        var game = NewInfiniteGame();

        Assert.Equal(GamePhase.Playing, game.Phase);
        Assert.Equal(1, game.Level);
        Assert.Equal(0, game.Score);
        Assert.Equal(0, game.LinesCleared);
        Assert.NotNull(game.Current);
        Assert.All(game.Board.AllCells(), cell => Assert.True(cell.IsEmpty));
        Assert.Contains(GameSound.GameStart, game.DrainSounds());
    }

    [Fact]
    public void PieceFallsOneCellPerInterval()
    {
        var game = NewInfiniteGame();
        int startY = game.Current!.Y;

        game.Update(TimeSpan.FromSeconds(0.4));
        Assert.Equal(startY, game.Current!.Y);

        game.Update(TimeSpan.FromSeconds(0.4));
        Assert.Equal(startY + 1, game.Current!.Y);
    }

    [Fact]
    public void SoftDropFallsTenTimesFaster()
    {
        var game = NewInfiniteGame();
        int startY = game.Current!.Y;
        game.SoftDropping = true;

        game.Update(TimeSpan.FromSeconds(0.24));

        Assert.Equal(startY + 3, game.Current!.Y);
    }

    [Fact]
    public void HorizontalMovementStopsAtWalls()
    {
        var game = NewInfiniteGame();

        for (int i = 0; i < 10; i++)
        {
            game.MoveLeft();
        }

        Assert.Equal(0, game.Current!.X);
        Assert.False(game.MoveLeft());

        for (int i = 0; i < 20; i++)
        {
            game.MoveRight();
        }

        Assert.Equal(Board.Width - 2, game.Current!.X);
        Assert.False(game.MoveRight());
    }

    [Fact]
    public void RotationIsCancelledWhenItCollides()
    {
        var game = NewInfiniteGame(PieceType.I);
        game.Board[5, 2] = Cell.Block(PieceType.T);

        Assert.False(game.RotateClockwise());
        Assert.Equal(0, game.Current!.Rotation);

        game.Board[5, 2] = Cell.Empty;
        Assert.True(game.RotateClockwise());
        Assert.Equal(1, game.Current!.Rotation);
    }

    [Fact]
    public void GhostShowsLandingPositionAndFollowsMovement()
    {
        var game = NewInfiniteGame();

        Assert.Equal(Board.Height - 2, game.Ghost!.Y);
        Assert.Equal(game.Current!.X, game.Ghost!.X);

        game.MoveRight();
        Assert.Equal(game.Current!.X, game.Ghost!.X);

        BoardHelper.FillRow(game.Board, Board.Height - 1);
        Assert.Equal(Board.Height - 3, game.Ghost!.Y);
    }

    [Fact]
    public void HardDropLandsAtGhostPositionAndLocksImmediately()
    {
        var game = NewInfiniteGame();
        game.DrainSounds();

        game.HardDrop();

        Assert.Equal(CellKind.Block, game.Board[4, Board.Height - 1].Kind);
        Assert.Equal(CellKind.Block, game.Board[5, Board.Height - 1].Kind);
        Assert.Equal(CellKind.Block, game.Board[5, Board.Height - 2].Kind);
        Assert.Contains(GameSound.PieceLock, game.DrainSounds());
    }

    [Fact]
    public void HardDropAddsSmallScoreForDroppedDistance()
    {
        var game = NewInfiniteGame();
        game.HardDrop();

        Assert.Equal(18, game.Score);
    }

    [Fact]
    public void LandingSpawnsTheNextPiece()
    {
        var game = NewInfiniteGame(PieceType.O, PieceType.T);
        var next = game.NextPiece;

        for (int i = 0; i < 30; i++)
        {
            game.Update(TimeSpan.FromSeconds(0.8));
        }

        Assert.Equal(GamePhase.Playing, game.Phase);
        Assert.NotNull(game.Current);
        Assert.Equal(next, game.Current!.Type);
        Assert.Equal(CellKind.Block, game.Board[4, Board.Height - 1].Kind);
    }

    [Fact]
    public void CompletedRowEntersClearingPhaseThenClears()
    {
        var game = NewInfiniteGame();
        BoardHelper.FillRow(game.Board, Board.Height - 1);
        game.Board[4, Board.Height - 1] = Cell.Empty;
        game.Board[5, Board.Height - 1] = Cell.Empty;

        game.HardDrop();

        Assert.Equal(GamePhase.Clearing, game.Phase);
        Assert.Equal(new[] { Board.Height - 1 }, game.ClearingRows);

        game.Update(TimeSpan.FromSeconds(0.15));
        Assert.Equal(GamePhase.Clearing, game.Phase);
        Assert.InRange(game.ClearProgress, 0.4, 0.6);

        game.Update(TimeSpan.FromSeconds(0.16));
        Assert.Equal(GamePhase.Playing, game.Phase);
        Assert.Equal(1, game.LinesCleared);
        Assert.Empty(game.ClearingRows);
        Assert.Equal(CellKind.Block, game.Board[4, Board.Height - 1].Kind);
    }

    [Fact]
    public void InputIsDiscardedDuringClearingAnimation()
    {
        var game = NewInfiniteGame();
        BoardHelper.FillRow(game.Board, Board.Height - 1);
        game.Board[4, Board.Height - 1] = Cell.Empty;
        game.Board[5, Board.Height - 1] = Cell.Empty;
        game.HardDrop();

        Assert.Equal(GamePhase.Clearing, game.Phase);
        Assert.False(game.MoveLeft());
        Assert.False(game.MoveRight());
        Assert.False(game.RotateClockwise());
        game.HardDrop();
        Assert.Equal(GamePhase.Clearing, game.Phase);
    }

    [Theory]
    [InlineData(1, 40)]
    [InlineData(2, 100)]
    [InlineData(3, 300)]
    [InlineData(4, 1200)]
    public void ScoreFollowsLineCountTable(int lines, int expected)
    {
        var game = NewInfiniteGame(PieceType.I);

        // 下から `lines` 行を x=0 だけ空けて埋め、縦向きの I ピースで一気に消す。
        for (int i = 0; i < lines; i++)
        {
            BoardHelper.FillRowExcept(game.Board, Board.Height - 1 - i, 0);
        }

        game.RotateClockwise();
        while (game.MoveLeft())
        {
        }

        int beforeDrop = game.Score;
        game.HardDrop();
        int hardDropBonus = game.Score - beforeDrop;
        game.Update(TimeSpan.FromSeconds(0.31));

        Assert.Equal(lines, game.LinesCleared);
        Assert.Equal(expected + hardDropBonus, game.Score);
    }

    [Fact]
    public void ScoreIsMultipliedByCurrentLevel()
    {
        var game = NewInfiniteGame();
        ClearSingleRows(game, TetrisGame.LinesPerLevel);

        Assert.Equal(2, game.Level);

        int before = game.Score;
        BoardHelper.FillRow(game.Board, Board.Height - 1);
        game.Board[4, Board.Height - 1] = Cell.Empty;
        game.Board[5, Board.Height - 1] = Cell.Empty;
        int scoreBeforeDrop = game.Score;
        game.HardDrop();
        int hardDropBonus = game.Score - scoreBeforeDrop;
        game.Update(TimeSpan.FromSeconds(0.31));

        Assert.Equal(before + hardDropBonus + (40 * 2), game.Score);
    }

    [Fact]
    public void LevelIncreasesEveryTwentyLines()
    {
        var game = NewInfiniteGame();

        ClearSingleRows(game, 19);
        Assert.Equal(1, game.Level);

        ClearSingleRows(game, 1);
        Assert.Equal(2, game.Level);

        ClearSingleRows(game, 20);
        Assert.Equal(3, game.Level);
        Assert.Equal(40, game.LinesCleared);
    }

    [Theory]
    [InlineData(1, 800)]
    [InlineData(2, 720)]
    [InlineData(3, 648)]
    public void FallIntervalShrinksTenPercentPerLevel(int level, int expectedMilliseconds)
    {
        Assert.Equal(expectedMilliseconds, TetrisGame.ComputeFallInterval(level).TotalMilliseconds, 1);
    }

    [Fact]
    public void FallIntervalIsClampedAtFiftyMilliseconds()
    {
        Assert.Equal(TetrisGame.MinimumFallInterval, TetrisGame.ComputeFallInterval(40));
        Assert.Equal(TetrisGame.MinimumFallInterval, TetrisGame.ComputeFallInterval(200));
    }

    [Fact]
    public void PauseFreezesTimeAndInputUntilResumed()
    {
        var game = NewInfiniteGame();
        int y = game.Current!.Y;

        game.TogglePause();
        Assert.Equal(GamePhase.Paused, game.Phase);

        game.Update(TimeSpan.FromSeconds(5));
        Assert.Equal(TimeSpan.Zero, game.Elapsed);
        Assert.Equal(y, game.Current!.Y);
        Assert.False(game.MoveLeft());
        Assert.False(game.RotateClockwise());

        game.TogglePause();
        Assert.Equal(GamePhase.Playing, game.Phase);
        game.Update(TimeSpan.FromSeconds(0.8));
        Assert.Equal(y + 1, game.Current!.Y);
        Assert.Equal(TimeSpan.FromSeconds(0.8), game.Elapsed);
    }

    [Fact]
    public void PauseDuringClearingResumesClearing()
    {
        var game = NewInfiniteGame();
        BoardHelper.FillRow(game.Board, Board.Height - 1);
        game.Board[4, Board.Height - 1] = Cell.Empty;
        game.Board[5, Board.Height - 1] = Cell.Empty;
        game.HardDrop();

        game.TogglePause();
        Assert.Equal(GamePhase.Paused, game.Phase);
        game.Update(TimeSpan.FromSeconds(1));
        Assert.Equal(GamePhase.Paused, game.Phase);

        game.TogglePause();
        Assert.Equal(GamePhase.Clearing, game.Phase);
        game.Update(TimeSpan.FromSeconds(0.31));
        Assert.Equal(GamePhase.Playing, game.Phase);
    }

    [Fact]
    public void GameOverWhenSpawnPositionIsOccupied()
    {
        var game = NewInfiniteGame();
        game.Board[4, 0] = Cell.Block(PieceType.T);

        game.HardDrop();

        Assert.Equal(GamePhase.GameOver, game.Phase);
        Assert.False(game.IsActive);
        game.Update(TimeSpan.FromSeconds(5));
        Assert.Equal(GamePhase.GameOver, game.Phase);
    }

    [Fact]
    public void RestartResetsBoardScoreAndPhase()
    {
        var game = NewInfiniteGame();
        game.HardDrop();
        game.Update(TimeSpan.FromSeconds(1));
        Assert.True(game.Score > 0);

        game.Restart();

        Assert.Equal(GamePhase.Playing, game.Phase);
        Assert.Equal(0, game.Score);
        Assert.Equal(1, game.Level);
        Assert.Equal(TimeSpan.Zero, game.Elapsed);
        Assert.All(game.Board.AllCells(), cell => Assert.True(cell.IsEmpty));
    }

    [Fact]
    public void ElapsedTracksPlayTime()
    {
        var game = NewInfiniteGame();
        game.Update(TimeSpan.FromSeconds(1.5));
        game.Update(TimeSpan.FromSeconds(0.5));

        Assert.Equal(TimeSpan.FromSeconds(2), game.Elapsed);
        Assert.Equal("00:02:00", TimeDisplay.Format(game.Elapsed));
    }

    /// <summary>盤面をリセットしつつ 1 行消しを指定回数繰り返す。</summary>
    private static void ClearSingleRows(TetrisGame game, int times)
    {
        for (int i = 0; i < times; i++)
        {
            game.Board.Clear();
            BoardHelper.FillRow(game.Board, Board.Height - 1);
            game.Board[4, Board.Height - 1] = Cell.Empty;
            game.Board[5, Board.Height - 1] = Cell.Empty;
            game.HardDrop();
            game.Update(TimeSpan.FromSeconds(0.31));
        }
    }
}
