using System.Collections.Generic;
using System.Windows.Media;
using Tetris.Core;

namespace Tetris.App;

/// <summary>セル種別・ピース種別ごとの固定色。</summary>
public static class PieceColors
{
    private static readonly Dictionary<PieceType, SolidColorBrush> Brushes = new()
    {
        [PieceType.I] = Freeze(Color.FromRgb(0x2E, 0xC4, 0xE6)),
        [PieceType.J] = Freeze(Color.FromRgb(0x36, 0x6C, 0xE0)),
        [PieceType.L] = Freeze(Color.FromRgb(0xE8, 0x8B, 0x2A)),
        [PieceType.O] = Freeze(Color.FromRgb(0xE6, 0xC9, 0x2E)),
        [PieceType.S] = Freeze(Color.FromRgb(0x4C, 0xC2, 0x5B)),
        [PieceType.Z] = Freeze(Color.FromRgb(0xE0, 0x45, 0x45)),
        [PieceType.T] = Freeze(Color.FromRgb(0xA9, 0x54, 0xE0)),
    };

    public static readonly SolidColorBrush Wall = Freeze(Color.FromRgb(0x76, 0x7C, 0x8C));

    public static readonly SolidColorBrush Gem = Freeze(Color.FromRgb(0xFF, 0x4F, 0xA3));

    public static readonly SolidColorBrush Empty = Freeze(Color.FromArgb(0x00, 0, 0, 0));

    public static readonly SolidColorBrush GridLine = Freeze(Color.FromRgb(0x25, 0x27, 0x33));

    public static SolidColorBrush For(PieceType type) => Brushes[type];

    public static Brush For(Cell cell) => cell.Kind switch
    {
        CellKind.Block => For(cell.Piece),
        CellKind.Wall => Wall,
        CellKind.Gem => Gem,
        _ => Empty,
    };

    private static SolidColorBrush Freeze(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
