namespace Tetris.Core;

public enum CellKind
{
    Empty,
    Piece,
    Wall,
    Gem,
}

/// <summary>盤面 1 マスの内容。</summary>
public readonly record struct Cell(CellKind Kind, PieceType Piece = PieceType.I)
{
    public static readonly Cell Empty = new(CellKind.Empty);
    public static readonly Cell Wall = new(CellKind.Wall);
    public static readonly Cell Gem = new(CellKind.Gem);

    public static Cell OfPiece(PieceType type) => new(CellKind.Piece, type);

    public bool IsOccupied => Kind != CellKind.Empty;
}
