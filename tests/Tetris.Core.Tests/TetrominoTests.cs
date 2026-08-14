using Tetris.Core;

namespace Tetris.Core.Tests;

public class TetrominoTests
{
    [Theory]
    [InlineData(PieceType.I, 4)]
    [InlineData(PieceType.J, 3)]
    [InlineData(PieceType.L, 3)]
    [InlineData(PieceType.O, 2)]
    [InlineData(PieceType.S, 3)]
    [InlineData(PieceType.Z, 3)]
    [InlineData(PieceType.T, 3)]
    public void EveryPieceHasFourCellsInEveryRotation(PieceType type, int size)
    {
        var tetromino = Tetromino.Get(type);

        Assert.Equal(size, tetromino.Size);
        for (var rotation = 0; rotation < 4; rotation++)
        {
            Assert.Equal(4, tetromino.Cells(rotation).Count);
        }
    }

    [Fact]
    public void FourRotationsReturnToOriginalShape()
    {
        foreach (var type in Tetromino.AllTypes)
        {
            var tetromino = Tetromino.Get(type);
            Assert.Equal(tetromino.Cells(0), tetromino.Cells(4));
        }
    }

    [Fact]
    public void OPieceIsRotationInvariant()
    {
        var o = Tetromino.Get(PieceType.O);

        for (var rotation = 1; rotation < 4; rotation++)
        {
            Assert.Equal(o.Cells(0), o.Cells(rotation));
        }
    }

    [Fact]
    public void IPieceRotatesFromHorizontalToVertical()
    {
        var i = Tetromino.Get(PieceType.I);

        Assert.All(i.Cells(0), cell => Assert.Equal(1, cell.Y));
        Assert.All(i.Cells(1), cell => Assert.Equal(2, cell.X));
    }

    [Fact]
    public void TPieceRotatesClockwise()
    {
        var t = Tetromino.Get(PieceType.T);

        Assert.Equal(
            new[] { new Offset(1, 0), new Offset(0, 1), new Offset(1, 1), new Offset(2, 1) },
            t.Cells(0));
        Assert.Equal(
            new[] { new Offset(1, 0), new Offset(1, 1), new Offset(2, 1), new Offset(1, 2) },
            t.Cells(1));
    }

    [Fact]
    public void BoardCellsAreOffsetByPiecePosition()
    {
        var piece = new Piece(PieceType.O, 0, 4, 5);

        Assert.Equal(
            new[] { new Offset(4, 5), new Offset(5, 5), new Offset(4, 6), new Offset(5, 6) },
            piece.BoardCells().ToArray());
    }
}
