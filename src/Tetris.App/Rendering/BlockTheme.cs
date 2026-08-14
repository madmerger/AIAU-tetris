using System.Collections.Generic;
using System.Windows.Media;
using Tetris.Core;

namespace Tetris.App.Rendering;

/// <summary>ブロックの配色。ピースごとに固定色を持つ。</summary>
public static class BlockTheme
{
    public static readonly Brush Background = Freeze(new SolidColorBrush(Color.FromRgb(0x0C, 0x0E, 0x16)));
    public static readonly Brush GridLine = Freeze(new SolidColorBrush(Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF)));
    public static readonly Brush BoardBorder = Freeze(new SolidColorBrush(Color.FromRgb(0x3C, 0x44, 0x62)));
    public static readonly Brush GhostFill = Freeze(new SolidColorBrush(Color.FromArgb(0x50, 0xFF, 0xFF, 0xFF)));
    public static readonly Brush OverlayScrim = Freeze(new SolidColorBrush(Color.FromArgb(0xC0, 0x0A, 0x0C, 0x14)));
    public static readonly Brush Text = Freeze(new SolidColorBrush(Color.FromRgb(0xEA, 0xEE, 0xF7)));
    public static readonly Brush Accent = Freeze(new SolidColorBrush(Color.FromRgb(0x4C, 0xC9, 0xF0)));

    private static readonly Dictionary<PieceType, Brush> PieceBrushes = new()
    {
        [PieceType.I] = Freeze(new SolidColorBrush(Color.FromRgb(0x31, 0xC8, 0xE0))),
        [PieceType.J] = Freeze(new SolidColorBrush(Color.FromRgb(0x35, 0x6B, 0xE8))),
        [PieceType.L] = Freeze(new SolidColorBrush(Color.FromRgb(0xEF, 0x8C, 0x2B))),
        [PieceType.O] = Freeze(new SolidColorBrush(Color.FromRgb(0xEB, 0xCB, 0x2E))),
        [PieceType.S] = Freeze(new SolidColorBrush(Color.FromRgb(0x4A, 0xC4, 0x6A))),
        [PieceType.Z] = Freeze(new SolidColorBrush(Color.FromRgb(0xE0, 0x43, 0x4C))),
        [PieceType.T] = Freeze(new SolidColorBrush(Color.FromRgb(0xA5, 0x5C, 0xE0))),
    };

    private static readonly Brush WallBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x6D, 0x74, 0x8C)));
    private static readonly Brush GemBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xF0, 0x4C, 0xB0)));

    public static Brush ForCell(Cell cell) => cell.Kind switch
    {
        CellKind.Wall => WallBrush,
        CellKind.Gem => GemBrush,
        CellKind.Piece => PieceBrushes[cell.Piece],
        _ => Background,
    };

    public static Brush ForPiece(PieceType type) => PieceBrushes[type];

    private static Brush Freeze(SolidColorBrush brush)
    {
        brush.Freeze();
        return brush;
    }
}
