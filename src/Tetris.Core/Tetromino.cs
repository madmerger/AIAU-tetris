using System.Collections.ObjectModel;

namespace Tetris.Core;

/// <summary>各ピースの形状と 90 度単位の回転形を保持する。</summary>
public sealed class Tetromino
{
    private static readonly Dictionary<PieceType, Tetromino> Table = Build();

    private readonly bool[][,] _rotations;
    private readonly ReadOnlyCollection<Offset>[] _cells;

    private Tetromino(PieceType type, string[] shape)
    {
        Type = type;
        Size = shape.Length;
        _rotations = new bool[4][,];
        _cells = new ReadOnlyCollection<Offset>[4];

        var current = ToMatrix(shape);
        for (var r = 0; r < 4; r++)
        {
            _rotations[r] = current;
            _cells[r] = new ReadOnlyCollection<Offset>(ToOffsets(current));
            current = RotateClockwise(current);
        }
    }

    public PieceType Type { get; }

    /// <summary>形状を収める正方行列の一辺。</summary>
    public int Size { get; }

    public static Tetromino Get(PieceType type) => Table[type];

    public static IEnumerable<PieceType> AllTypes => Enum.GetValues<PieceType>();

    /// <summary>指定回転での占有セル（行列内の相対座標）。</summary>
    public IReadOnlyList<Offset> Cells(int rotation) => _cells[Normalize(rotation)];

    public bool IsFilled(int rotation, int x, int y)
    {
        var matrix = _rotations[Normalize(rotation)];
        return x >= 0 && y >= 0 && x < Size && y < Size && matrix[y, x];
    }

    internal static int Normalize(int rotation) => ((rotation % 4) + 4) % 4;

    private static bool[,] ToMatrix(string[] shape)
    {
        var size = shape.Length;
        var matrix = new bool[size, size];
        for (var y = 0; y < size; y++)
        {
            if (shape[y].Length != size)
            {
                throw new ArgumentException("形状は正方行列でなければならない。", nameof(shape));
            }

            for (var x = 0; x < size; x++)
            {
                matrix[y, x] = shape[y][x] == 'X';
            }
        }

        return matrix;
    }

    private static List<Offset> ToOffsets(bool[,] matrix)
    {
        var size = matrix.GetLength(0);
        var offsets = new List<Offset>(4);
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                if (matrix[y, x])
                {
                    offsets.Add(new Offset(x, y));
                }
            }
        }

        return offsets;
    }

    private static bool[,] RotateClockwise(bool[,] source)
    {
        var size = source.GetLength(0);
        var result = new bool[size, size];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                result[y, x] = source[size - 1 - x, y];
            }
        }

        return result;
    }

    private static Dictionary<PieceType, Tetromino> Build()
    {
        var definitions = new Dictionary<PieceType, string[]>
        {
            [PieceType.I] = new[]
            {
                "....",
                "XXXX",
                "....",
                "....",
            },
            [PieceType.J] = new[]
            {
                "X..",
                "XXX",
                "...",
            },
            [PieceType.L] = new[]
            {
                "..X",
                "XXX",
                "...",
            },
            [PieceType.O] = new[]
            {
                "XX",
                "XX",
            },
            [PieceType.S] = new[]
            {
                ".XX",
                "XX.",
                "...",
            },
            [PieceType.Z] = new[]
            {
                "XX.",
                ".XX",
                "...",
            },
            [PieceType.T] = new[]
            {
                ".X.",
                "XXX",
                "...",
            },
        };

        return definitions.ToDictionary(pair => pair.Key, pair => new Tetromino(pair.Key, pair.Value));
    }
}

/// <summary>ピース行列内の相対座標。</summary>
public readonly record struct Offset(int X, int Y);
