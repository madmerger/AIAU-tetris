using System;
using System.Windows;
using System.Windows.Media;
using Tetris.Core;

namespace Tetris.App.Rendering;

/// <summary>次のピース 1 個を表示するプレビュー枠。</summary>
public sealed class NextPreview : FrameworkElement
{
    private readonly Pen _borderPen;

    public NextPreview()
    {
        _borderPen = new Pen(BlockTheme.BoardBorder, 1.0);
        _borderPen.Freeze();
    }

    public PieceType? Piece { get; set; }

    protected override void OnRender(DrawingContext dc)
    {
        var size = RenderSize;
        dc.DrawRectangle(BlockTheme.Background, _borderPen, new Rect(0, 0, size.Width, size.Height));

        if (Piece is not { } type)
        {
            return;
        }

        var shape = Tetromino.Get(type);
        var cells = shape.Cells(0);
        var minX = int.MaxValue;
        var maxX = int.MinValue;
        var minY = int.MaxValue;
        var maxY = int.MinValue;
        foreach (var cell in cells)
        {
            minX = Math.Min(minX, cell.X);
            maxX = Math.Max(maxX, cell.X);
            minY = Math.Min(minY, cell.Y);
            maxY = Math.Max(maxY, cell.Y);
        }

        var widthCells = maxX - minX + 1;
        var heightCells = maxY - minY + 1;
        var cellSize = Math.Min((size.Width - 12) / widthCells, (size.Height - 12) / heightCells);
        var originX = (size.Width - (cellSize * widthCells)) / 2;
        var originY = (size.Height - (cellSize * heightCells)) / 2;

        var brush = BlockTheme.ForPiece(type);
        var inset = Math.Max(cellSize * 0.06, 0.5);
        foreach (var cell in cells)
        {
            var rect = new Rect(
                originX + ((cell.X - minX) * cellSize) + inset,
                originY + ((cell.Y - minY) * cellSize) + inset,
                cellSize - (inset * 2),
                cellSize - (inset * 2));
            dc.DrawRectangle(brush, null, rect);
        }
    }
}
