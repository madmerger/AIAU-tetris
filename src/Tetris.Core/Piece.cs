using System.Collections.Generic;

namespace Tetris.Core;

/// <summary>盤面上に配置された（あるいは配置しようとしている）ピース。</summary>
public sealed class Piece
{
    public Piece(PieceType type, int x, int y, int rotation = 0)
    {
        Type = type;
        X = x;
        Y = y;
        Rotation = ((rotation % 4) + 4) % 4;
    }

    public PieceType Type { get; }

    /// <summary>形状行列左上の盤面 X 座標。</summary>
    public int X { get; private set; }

    /// <summary>形状行列左上の盤面 Y 座標（下方向が正）。</summary>
    public int Y { get; private set; }

    public int Rotation { get; private set; }

    public Piece Moved(int dx, int dy) => new(Type, X + dx, Y + dy, Rotation);

    public Piece Rotated(int steps) => new(Type, X, Y, Rotation + steps);

    public void ApplyFrom(Piece other)
    {
        X = other.X;
        Y = other.Y;
        Rotation = other.Rotation;
    }

    /// <summary>ピースが占める盤面座標を列挙する。</summary>
    public IEnumerable<(int X, int Y)> Cells()
    {
        var shape = Tetromino.Shape(Type, Rotation);
        int size = shape.GetLength(0);
        for (int r = 0; r < size; r++)
        {
            for (int c = 0; c < size; c++)
            {
                if (shape[r, c])
                {
                    yield return (X + c, Y + r);
                }
            }
        }
    }
}
