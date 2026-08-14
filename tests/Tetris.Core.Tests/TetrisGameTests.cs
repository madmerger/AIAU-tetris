using Tetris.Core;

namespace Tetris.Core.Tests;

public class TetrisGameTests
{
    private static TetrisGame NewInfiniteGame(params PieceType[] sequence)
        => new(GameMode.Infinite, new FixedPieceGenerator(sequence.Length == 0 ? new[] { PieceType.I } : sequence));

    [Fact]
    public void GameStartsPlayingWithSpawnedAndNextPiece()
    {
        var game = NewInfiniteGame(PieceType.I, PieceType.T);

        Assert.Equal(GamePhase.Playing, game.Phase);
        Assert.Equal(PieceType.I, game.Current.Type);
        Assert.Equal(PieceType.T, game.NextPiece);
        Assert.Equal(3, game.Current.X);
        Assert.Equal(0, game.Current.Y);
        Assert.Equal(1, game.Level);
        Assert.Equal(0, game.Score);
    }

    [Fact]
    public void PieceFallsOneCellPerInterval()
    {
        var game = NewInfiniteGame();

        game.Update(0.79);
        Assert.Equal(0, game.Current.Y);

        game.Update(0.02);
        Assert.Equal(1, game.Current.Y);
    }

    [Fact]
    public void SoftDropFallsTenTimesFaster()
    {
        var game = NewInfiniteGame();
        game.SoftDropping = true;

        game.Update(0.08);

        Assert.Equal(1, game.Current.Y);
    }

    [Fact]
    public void SoftDropDoesNotConsumeAccumulatedFallTimer()
    {
        var game = NewInfiniteGame();

        game.Update(0.7);
        Assert.Equal(0, game.Current.Y);

        game.SoftDropping = true;
        game.Update(0.02);

        Assert.Equal(1, game.Current.Y);
    }

    [Fact]
    public void MovementStopsAtWalls()
    {
        var game = NewInfiniteGame(PieceType.O);

        while (game.MoveLeft())
        {
        }

        Assert.Equal(0, game.Current.X);

        while (game.MoveRight())
        {
        }

        Assert.Equal(8, game.Current.X);
    }

    [Fact]
    public void RotationIsCancelledWhenItCollides()
    {
        var game = NewInfiniteGame();

        // 縦向き I の下 3 マスを塞ぐと、回転できない。
        for (var y = 1; y <= 3; y++)
        {
            game.Board[5, y] = Cell.Wall;
        }

        Assert.False(game.RotateClockwise());
        Assert.Equal(0, game.Current.Rotation);
    }

    [Fact]
    public void RotationChangesOrientationWhenThereIsRoom()
    {
        var game = NewInfiniteGame();

        Assert.True(game.RotateClockwise());
        Assert.Equal(1, game.Current.Rotation);
        Assert.All(game.Current.BoardCells(), cell => Assert.Equal(5, cell.X));
    }

    [Fact]
    public void GhostPositionFollowsCurrentPiece()
    {
        var game = NewInfiniteGame(PieceType.O);

        Assert.Equal(18, game.GhostY);

        game.Board[4, 19] = Cell.Wall;
        Assert.Equal(17, game.GhostY);
    }

    [Fact]
    public void HardDropLocksImmediatelyAndScoresDistance()
    {
        var game = NewInfiniteGame(PieceType.O, PieceType.T);

        var distance = game.GhostY - game.Current.Y;
        game.HardDrop();

        Assert.Equal(distance, game.Score);
        Assert.True(game.Board.IsOccupied(4, 19));
        Assert.Equal(PieceType.T, game.Current.Type);
        Assert.Contains(GameEventType.PieceLocked, game.DrainEvents());
    }

    [Fact]
    public void SingleLineClearScoresAndCollapsesAfterAnimation()
    {
        var game = NewInfiniteGame();
        FillBottomRowExceptColumn(game.Board, 19, hole: 0);
        game.Board[3, 18] = Cell.OfPiece(PieceType.T);

        DropVerticalIAt(game, column: 0, finishAnimation: false);

        Assert.Equal(GamePhase.LineClearing, game.Phase);
        Assert.Equal(new[] { 19 }, game.ClearingRows);
        Assert.Equal(40 + 16, game.Score); // 1 行消去 (40 x Lv1) + ハードドロップ加点
        Assert.Equal(1, game.TotalLines);
        Assert.True(game.Board.IsOccupied(5, 19)); // 演出中は消えていない

        game.Update(0.15);
        Assert.Equal(GamePhase.LineClearing, game.Phase);
        Assert.InRange(game.ClearProgress, 0.4, 0.6);

        game.Update(0.15);
        Assert.Equal(GamePhase.Playing, game.Phase);
        Assert.False(game.Board.IsOccupied(5, 19));
        Assert.Empty(game.ClearingRows);
        Assert.True(game.Board.IsOccupied(3, 19)); // 上のブロックが 1 段下がる
    }

    [Fact]
    public void FourLineClearScores1200TimesLevel()
    {
        var game = NewInfiniteGame();
        for (var y = 16; y <= 19; y++)
        {
            FillBottomRowExceptColumn(game.Board, y, hole: 0);
        }

        DropVerticalIAt(game, column: 0, finishAnimation: false);

        Assert.Equal(4, game.ClearingRows.Count);
        Assert.Equal(1200 + 16, game.Score);
        Assert.Equal(4, game.TotalLines);
    }

    [Fact]
    public void InputIsDiscardedDuringLineClearAnimation()
    {
        var game = NewInfiniteGame();
        FillBottomRowExceptColumn(game.Board, 19, hole: 0);
        DropVerticalIAt(game, column: 0, finishAnimation: false);

        Assert.Equal(GamePhase.LineClearing, game.Phase);
        Assert.False(game.MoveLeft());
        Assert.False(game.MoveRight());
        Assert.False(game.RotateClockwise());
        Assert.False(game.HardDrop());
    }

    [Fact]
    public void LevelIncreasesAfter20ClearedLines()
    {
        var game = NewInfiniteGame();

        for (var i = 0; i < 5; i++)
        {
            for (var y = 16; y <= 19; y++)
            {
                FillBottomRowExceptColumn(game.Board, y, hole: 0);
            }

            DropVerticalIAt(game, column: 0);
        }

        Assert.Equal(20, game.TotalLines);
        Assert.Equal(2, game.Level);
    }

    [Fact]
    public void PauseStopsProgressAndInput()
    {
        var game = NewInfiniteGame();

        game.TogglePause();
        Assert.Equal(GamePhase.Paused, game.Phase);

        game.Update(5.0);
        Assert.Equal(0, game.Current.Y);
        Assert.Equal(0, game.ElapsedSeconds);
        Assert.False(game.MoveLeft());

        game.TogglePause();
        Assert.Equal(GamePhase.Playing, game.Phase);
        Assert.True(game.MoveLeft());
    }

    [Fact]
    public void ElapsedTimeAdvancesWhilePlaying()
    {
        var game = NewInfiniteGame();

        game.Update(0.5);
        game.Update(0.25);

        Assert.Equal(0.75, game.ElapsedSeconds, 6);
    }

    [Fact]
    public void GameOverWhenSpawnPositionIsOccupied()
    {
        var game = NewInfiniteGame();
        game.HardDrop();

        for (var x = 3; x <= 6; x++)
        {
            game.Board[x, 1] = Cell.Wall;
        }

        game.HardDrop();

        Assert.Equal(GamePhase.GameOver, game.Phase);
        Assert.Contains(GameEventType.GameOver, game.DrainEvents());

        game.Update(1.0);
        Assert.Equal(GamePhase.GameOver, game.Phase);
        Assert.False(game.MoveLeft());
    }

    [Fact]
    public void RestartResetsScoreAndBoard()
    {
        var game = NewInfiniteGame();
        game.HardDrop();
        game.Update(1.0);

        game.Restart();

        Assert.Equal(GamePhase.Playing, game.Phase);
        Assert.Equal(0, game.Score);
        Assert.Equal(0, game.ElapsedSeconds);
        Assert.Equal(0, game.Current.Y);
        for (var x = 0; x < game.Board.Width; x++)
        {
            Assert.False(game.Board.IsOccupied(x, 19));
        }
    }

    [Fact]
    public void DrainEventsClearsQueue()
    {
        var game = NewInfiniteGame();
        game.HardDrop();

        Assert.NotEmpty(game.DrainEvents());
        Assert.Empty(game.DrainEvents());
    }

    internal static void FillBottomRowExceptColumn(Board board, int y, int hole)
    {
        for (var x = 0; x < board.Width; x++)
        {
            board[x, y] = x == hole ? Cell.Empty : Cell.OfPiece(PieceType.J);
        }
    }

    /// <summary>現在の I ピースを縦向きにして指定列へ寄せ、ハードドロップする。</summary>
    internal static void DropVerticalIAt(TetrisGame game, int column, bool finishAnimation = true)
    {
        Assert.Equal(PieceType.I, game.Current.Type);
        Assert.True(game.RotateClockwise());

        if (column <= 4)
        {
            while (game.Current.BoardCells().First().X > column && game.MoveLeft())
            {
            }
        }
        else
        {
            while (game.Current.BoardCells().First().X < column && game.MoveRight())
            {
            }
        }

        Assert.Equal(column, game.Current.BoardCells().First().X);
        game.HardDrop();

        if (finishAnimation && game.Phase == GamePhase.LineClearing)
        {
            game.Update(GameRules.LineClearAnimationSeconds);
        }
    }
}
