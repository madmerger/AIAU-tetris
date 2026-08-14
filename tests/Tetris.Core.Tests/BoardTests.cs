using Tetris.Core;

namespace Tetris.Core.Tests;

public class BoardTests
{
    [Fact]
    public void NewBoardIsEmpty()
    {
        var board = new Board();
        Assert.All(board.AllCells(), cell => Assert.True(cell.IsEmpty));
        Assert.Empty(board.FindFullRows());
    }

    [Fact]
    public void OutsideCoordinatesAreTreatedAsWall()
    {
        var board = new Board();
        Assert.Equal(CellKind.Wall, board[-1, 0].Kind);
        Assert.Equal(CellKind.Wall, board[Board.Width, 0].Kind);
        Assert.Equal(CellKind.Wall, board[0, Board.Height].Kind);
    }

    [Fact]
    public void CanPlaceRejectsOutOfBoundsAndOccupiedCells()
    {
        var board = new Board();
        Assert.True(board.CanPlace(new Piece(PieceType.O, 0, 0)));
        Assert.False(board.CanPlace(new Piece(PieceType.O, -1, 0)));
        Assert.False(board.CanPlace(new Piece(PieceType.O, Board.Width - 1, 0)));
        Assert.False(board.CanPlace(new Piece(PieceType.O, 0, Board.Height - 1)));

        board[1, 1] = Cell.Block(PieceType.T);
        Assert.False(board.CanPlace(new Piece(PieceType.O, 0, 0)));
    }

    [Fact]
    public void CellsAboveTheBoardAreTreatedAsEmptySpace()
    {
        var board = new Board();

        Assert.True(board.CanPlace(new Piece(PieceType.I, 3, -1, rotation: 1)));

        board[5, 0] = Cell.Block(PieceType.T);
        Assert.False(board.CanPlace(new Piece(PieceType.I, 3, -1, rotation: 1)));
    }

    [Fact]
    public void LockWritesPieceCells()
    {
        var board = new Board();
        board.Lock(new Piece(PieceType.O, 3, 18));

        Assert.Equal(Cell.Block(PieceType.O), board[3, 18]);
        Assert.Equal(Cell.Block(PieceType.O), board[4, 19]);
        Assert.True(board[5, 19].IsEmpty);
    }

    [Fact]
    public void FindFullRowsDetectsCompletedLines()
    {
        var board = new Board();
        BoardHelper.FillRow(board, 19);
        BoardHelper.FillRow(board, 17);
        board[3, 17] = Cell.Empty;

        Assert.Equal(new[] { 19 }, board.FindFullRows());
    }

    [Fact]
    public void ClearRowsShiftsRowsAboveDownwards()
    {
        var board = new Board();
        BoardHelper.FillRow(board, 19);
        BoardHelper.FillRow(board, 18);
        board[0, 17] = Cell.Block(PieceType.T);

        board.ClearRows(new[] { 18, 19 });

        Assert.All(board.FindFullRows(), _ => Assert.Fail("消去後に埋まった行が残っています"));
        Assert.Equal(CellKind.Block, board[0, 19].Kind);
        Assert.True(board[1, 19].IsEmpty);
        Assert.True(board[0, 17].IsEmpty);
    }

    [Fact]
    public void ClearRowsHandlesNonAdjacentRows()
    {
        var board = new Board();
        BoardHelper.FillRow(board, 19);
        BoardHelper.FillRow(board, 17);
        board[0, 18] = Cell.Gem;

        board.ClearRows(new[] { 17, 19 });

        Assert.Equal(CellKind.Gem, board[0, 19].Kind);
        Assert.Equal(0, board.CountGems() - 1);
    }

    [Fact]
    public void WallBlocksAreClearedLikeNormalBlocks()
    {
        var board = new Board();
        BoardHelper.FillRow(board, 19, Cell.Wall);

        Assert.Equal(new[] { 19 }, board.FindFullRows());
        board.ClearRows(new[] { 19 });
        Assert.All(board.AllCells(), cell => Assert.True(cell.IsEmpty));
    }

    [Fact]
    public void CountGemsCountsOnlyGems()
    {
        var board = new Board();
        board[0, 0] = Cell.Gem;
        board[1, 0] = Cell.Gem;
        board[2, 0] = Cell.Wall;
        board[3, 0] = Cell.Block(PieceType.S);

        Assert.Equal(2, board.CountGems());
    }
}

internal static class BoardHelper
{
    public static void FillRow(Board board, int y, Cell? cell = null)
    {
        for (int x = 0; x < Board.Width; x++)
        {
            board[x, y] = cell ?? Cell.Block(PieceType.I);
        }
    }

    /// <summary>指定行を 1 マスだけ空けて埋める。</summary>
    public static void FillRowExcept(Board board, int y, int emptyX, Cell? cell = null)
    {
        FillRow(board, y, cell);
        board[emptyX, y] = Cell.Empty;
    }
}
