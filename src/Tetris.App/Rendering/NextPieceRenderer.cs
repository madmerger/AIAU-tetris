using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using Tetris.Core;

namespace Tetris.App.Rendering;

/// <summary>ネクストピースのプレビュー描画（4×4 の矩形プールを使い回す）。</summary>
public sealed class NextPieceRenderer
{
    private const int Size = 4;

    private readonly Rectangle[,] _cells = new Rectangle[Size, Size];
    private PieceType? _rendered;

    public NextPieceRenderer(Canvas canvas, double cellSize)
    {
        canvas.Width = Size * cellSize;
        canvas.Height = Size * cellSize;

        for (int r = 0; r < Size; r++)
        {
            for (int c = 0; c < Size; c++)
            {
                var rect = new Rectangle
                {
                    Width = cellSize,
                    Height = cellSize,
                    RadiusX = 2,
                    RadiusY = 2,
                    Fill = PieceColors.Empty,
                    Visibility = Visibility.Collapsed,
                };
                Canvas.SetLeft(rect, c * cellSize);
                Canvas.SetTop(rect, r * cellSize);
                canvas.Children.Add(rect);
                _cells[r, c] = rect;
            }
        }
    }

    public void Render(PieceType type)
    {
        if (_rendered == type)
        {
            return;
        }

        _rendered = type;
        var shape = Tetromino.Shape(type, 0);
        int size = shape.GetLength(0);
        int offset = (Size - size) / 2;
        var brush = PieceColors.For(type);

        for (int r = 0; r < Size; r++)
        {
            for (int c = 0; c < Size; c++)
            {
                int sr = r - offset;
                int sc = c - offset;
                bool filled = sr >= 0 && sr < size && sc >= 0 && sc < size && shape[sr, sc];
                var rect = _cells[r, c];
                rect.Visibility = filled ? Visibility.Visible : Visibility.Collapsed;
                rect.Fill = brush;
            }
        }
    }
}
