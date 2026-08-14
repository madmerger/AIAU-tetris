using System.Collections.Generic;

namespace Tetris.Core;

public enum PieceType
{
    I,
    J,
    L,
    O,
    S,
    Z,
    T,
}

/// <summary>ピース形状の定義。回転は形状行列の 90 度回転（壁蹴りなし）。</summary>
public static class Tetromino
{
    public static readonly PieceType[] AllTypes =
    {
        PieceType.I, PieceType.J, PieceType.L, PieceType.O, PieceType.S, PieceType.Z, PieceType.T,
    };

    private static readonly Dictionary<PieceType, bool[][,]> Rotations = new();

    static Tetromino()
    {
        foreach (var (type, rows) in BaseShapes)
        {
            Rotations[type] = BuildRotations(Parse(rows));
        }
    }

    private static readonly (PieceType Type, string[] Rows)[] BaseShapes =
    {
        (PieceType.I, new[] { "....", "XXXX", "....", "...." }),
        (PieceType.J, new[] { "X..", "XXX", "..." }),
        (PieceType.L, new[] { "..X", "XXX", "..." }),
        (PieceType.O, new[] { "XX", "XX" }),
        (PieceType.S, new[] { ".XX", "XX.", "..." }),
        (PieceType.Z, new[] { "XX.", ".XX", "..." }),
        (PieceType.T, new[] { ".X.", "XXX", "..." }),
    };

    /// <summary>指定ピース・回転状態の形状行列（[row, col]、true が実体）。</summary>
    public static bool[,] Shape(PieceType type, int rotation) => Rotations[type][((rotation % 4) + 4) % 4];

    public static int Size(PieceType type) => Shape(type, 0).GetLength(0);

    private static bool[,] Parse(string[] rows)
    {
        int size = rows.Length;
        var shape = new bool[size, size];
        for (int r = 0; r < size; r++)
        {
            for (int c = 0; c < size; c++)
            {
                shape[r, c] = rows[r][c] == 'X';
            }
        }

        return shape;
    }

    private static bool[][,] BuildRotations(bool[,] baseShape)
    {
        var result = new bool[4][,];
        result[0] = baseShape;
        for (int i = 1; i < 4; i++)
        {
            result[i] = RotateClockwise(result[i - 1]);
        }

        return result;
    }

    private static bool[,] RotateClockwise(bool[,] shape)
    {
        int size = shape.GetLength(0);
        var rotated = new bool[size, size];
        for (int r = 0; r < size; r++)
        {
            for (int c = 0; c < size; c++)
            {
                rotated[c, size - 1 - r] = shape[r, c];
            }
        }

        return rotated;
    }
}
