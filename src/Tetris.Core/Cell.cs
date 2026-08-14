namespace Tetris.Core;

public enum CellKind : byte
{
    Empty = 0,
    Block = 1,
    Wall = 2,
    Gem = 3,
}

/// <summary>盤面 1 マスの内容。</summary>
public readonly record struct Cell(CellKind Kind, PieceType Piece)
{
    public static readonly Cell Empty = new(CellKind.Empty, PieceType.I);
    public static readonly Cell Wall = new(CellKind.Wall, PieceType.I);
    public static readonly Cell Gem = new(CellKind.Gem, PieceType.I);

    public static Cell Block(PieceType piece) => new(CellKind.Block, piece);

    public bool IsEmpty => Kind == CellKind.Empty;
}
