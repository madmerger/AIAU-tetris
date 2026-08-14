namespace Tetris.Core;

/// <summary>盤面上に配置された（あるいは配置を試みる）ピースの状態。</summary>
public readonly record struct Piece(PieceType Type, int Rotation, int X, int Y)
{
    public Tetromino Shape => Tetromino.Get(Type);

    public Piece Moved(int dx, int dy) => this with { X = X + dx, Y = Y + dy };

    public Piece RotatedClockwise() => this with { Rotation = Tetromino.Normalize(Rotation + 1) };

    /// <summary>盤面座標での占有セルを列挙する。</summary>
    public IEnumerable<Offset> BoardCells()
    {
        foreach (var cell in Shape.Cells(Rotation))
        {
            yield return new Offset(X + cell.X, Y + cell.Y);
        }
    }
}
