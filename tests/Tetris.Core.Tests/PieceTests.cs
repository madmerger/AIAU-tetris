using System.Linq;
using Tetris.Core;

namespace Tetris.Core.Tests;

public class PieceTests
{
    [Theory]
    [InlineData(PieceType.I, 4)]
    [InlineData(PieceType.J, 4)]
    [InlineData(PieceType.L, 4)]
    [InlineData(PieceType.O, 4)]
    [InlineData(PieceType.S, 4)]
    [InlineData(PieceType.Z, 4)]
    [InlineData(PieceType.T, 4)]
    public void EveryPieceHasFourCellsInEveryRotation(PieceType type, int expectedCells)
    {
        for (int rotation = 0; rotation < 4; rotation++)
        {
            var piece = new Piece(type, 0, 0, rotation);
            Assert.Equal(expectedCells, piece.Cells().Count());
        }
    }

    [Fact]
    public void RotationWrapsAroundEveryFourSteps()
    {
        var piece = new Piece(PieceType.T, 3, 5);
        var rotated = piece.Rotated(4);

        Assert.Equal(piece.Rotation, rotated.Rotation);
        Assert.Equal(piece.Cells().ToArray(), rotated.Cells().ToArray());
    }

    [Fact]
    public void OPieceIsUnchangedByRotation()
    {
        var baseCells = new Piece(PieceType.O, 0, 0).Cells().OrderBy(c => c.Y).ThenBy(c => c.X).ToArray();
        for (int rotation = 1; rotation < 4; rotation++)
        {
            var cells = new Piece(PieceType.O, 0, 0, rotation).Cells().OrderBy(c => c.Y).ThenBy(c => c.X).ToArray();
            Assert.Equal(baseCells, cells);
        }
    }

    [Fact]
    public void IPieceRotatesBetweenHorizontalAndVertical()
    {
        var horizontal = new Piece(PieceType.I, 0, 0).Cells().ToArray();
        Assert.Single(horizontal.Select(c => c.Y).Distinct());

        var vertical = new Piece(PieceType.I, 0, 0, 1).Cells().ToArray();
        Assert.Single(vertical.Select(c => c.X).Distinct());
    }

    [Fact]
    public void MovedDoesNotMutateOriginal()
    {
        var piece = new Piece(PieceType.L, 4, 4);
        var moved = piece.Moved(1, 2);

        Assert.Equal(4, piece.X);
        Assert.Equal(4, piece.Y);
        Assert.Equal(5, moved.X);
        Assert.Equal(6, moved.Y);
    }

    [Fact]
    public void SpawnPiecePlacesTopmostBlocksOnRowZero()
    {
        foreach (var type in Tetromino.AllTypes)
        {
            var piece = TetrisGame.CreateSpawnPiece(type);
            Assert.Equal(0, piece.Cells().Min(c => c.Y));
            Assert.All(piece.Cells(), cell => Assert.InRange(cell.X, 0, Board.Width - 1));
        }
    }
}
