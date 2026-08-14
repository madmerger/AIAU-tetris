using Tetris.Core;

namespace Tetris.Core.Tests;

public class BoardTests
{
    [Fact]
    public void NewBoardIsEmpty()
    {
        var board = new Board();

        Assert.Equal(10, board.Width);
        Assert.Equal(20, board.Height);
        for (var y = 0; y < board.Height; y++)
        {
            for (var x = 0; x < board.Width; x++)
            {
                Assert.False(board.IsOccupied(x, y));
            }
        }
    }

    [Fact]
    public void CanPlaceRejectsOutOfBoundsAndOverlap()
    {
        var board = new Board();
        board[0, 19] = Cell.Wall;

        Assert.False(board.CanPlace(new Piece(PieceType.O, 0, -1, 0)));
        Assert.False(board.CanPlace(new Piece(PieceType.O, 0, 9, 0)));
        Assert.False(board.CanPlace(new Piece(PieceType.O, 0, 0, 19)));
        Assert.False(board.CanPlace(new Piece(PieceType.O, 0, 0, 18)));
        Assert.True(board.CanPlace(new Piece(PieceType.O, 0, 4, 0)));
    }

    [Fact]
    public void LockWritesPieceCells()
    {
        var board = new Board();

        board.Lock(new Piece(PieceType.O, 0, 4, 18));

        Assert.True(board.IsOccupied(4, 18));
        Assert.True(board.IsOccupied(5, 19));
        Assert.Equal(CellKind.Piece, board[4, 19].Kind);
        Assert.Equal(PieceType.O, board[4, 19].Piece);
        Assert.False(board.IsOccupied(3, 19));
    }

    [Fact]
    public void DropYFindsLandingPosition()
    {
        var board = new Board();
        for (var x = 0; x < board.Width; x++)
        {
            board[x, 19] = Cell.Wall;
        }

        // O ピースは 2x2 なので、埋まった 19 行目の上（17,18 行）に着地する。
        Assert.Equal(17, board.DropY(new Piece(PieceType.O, 0, 4, 0)));
    }

    [Fact]
    public void FindFullRowsDetectsCompletedLines()
    {
        var board = new Board();
        FillRow(board, 19);
        FillRow(board, 17);
        board[3, 17] = Cell.Empty;

        Assert.Equal(new[] { 19 }, board.FindFullRows());
    }

    [Fact]
    public void ClearRowsCollapsesRowsAbove()
    {
        var board = new Board();
        FillRow(board, 19);
        FillRow(board, 18);
        board[2, 17] = Cell.OfPiece(PieceType.T);

        board.ClearRows(new[] { 18, 19 });

        Assert.True(board.IsOccupied(2, 19));
        Assert.Equal(PieceType.T, board[2, 19].Piece);
        Assert.False(board.IsOccupied(2, 17));
        Assert.False(board.IsOccupied(0, 18));
    }

    [Fact]
    public void ClearRowsRemovesWallsAndGems()
    {
        var board = new Board();
        FillRow(board, 19, Cell.Wall);
        board[5, 19] = Cell.Gem;

        Assert.Equal(1, board.CountGems());

        board.ClearRows(new[] { 19 });

        Assert.Equal(0, board.CountGems());
        Assert.False(board.IsOccupied(5, 19));
    }

    [Fact]
    public void LoadStageAppliesInitialLayout()
    {
        var board = new Board();
        var stage = StageData.Parse(1, StageRows("1112000000", "0000000002"));

        board.LoadStage(stage);

        Assert.Equal(CellKind.Wall, board[0, 18].Kind);
        Assert.Equal(CellKind.Gem, board[3, 18].Kind);
        Assert.Equal(CellKind.Gem, board[9, 19].Kind);
        Assert.Equal(2, board.CountGems());
    }

    internal static void FillRow(Board board, int y, Cell? cell = null)
    {
        for (var x = 0; x < board.Width; x++)
        {
            board[x, y] = cell ?? Cell.OfPiece(PieceType.I);
        }
    }

    /// <summary>下寄せで指定行を持つ 20 行ぶんのステージ行データを作る。</summary>
    internal static string[] StageRows(params string[] bottomRows)
    {
        var rows = new List<string>();
        for (var i = 0; i < Board.DefaultHeight - bottomRows.Length; i++)
        {
            rows.Add(new string('0', Board.DefaultWidth));
        }

        rows.AddRange(bottomRows);
        return rows.ToArray();
    }
}
