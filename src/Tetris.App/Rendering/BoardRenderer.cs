using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Shapes;
using Tetris.Core;

namespace Tetris.App.Rendering;

/// <summary>
/// 盤面描画。矩形要素は起動時に確保したプールを使い回し、毎フレーム塗りのみを更新する
/// （長時間プレイでも要素の生成・破棄が発生しない）。
/// </summary>
public sealed class BoardRenderer
{
    private const int MaxPieceCells = 4;

    private readonly double _cellSize;
    private readonly Rectangle[,] _cells = new Rectangle[Board.Height, Board.Width];
    private readonly Rectangle[] _ghostCells = new Rectangle[MaxPieceCells];
    private readonly Rectangle[] _pieceCells = new Rectangle[MaxPieceCells];

    public BoardRenderer(Canvas canvas, double cellSize)
    {
        _cellSize = cellSize;
        canvas.Width = Board.Width * cellSize;
        canvas.Height = Board.Height * cellSize;

        for (int y = 0; y < Board.Height; y++)
        {
            for (int x = 0; x < Board.Width; x++)
            {
                var rect = CreateRect();
                rect.Stroke = PieceColors.GridLine;
                rect.StrokeThickness = 1;
                Canvas.SetLeft(rect, x * cellSize);
                Canvas.SetTop(rect, y * cellSize);
                canvas.Children.Add(rect);
                _cells[y, x] = rect;
            }
        }

        for (int i = 0; i < MaxPieceCells; i++)
        {
            _ghostCells[i] = CreateRect();
            _ghostCells[i].Opacity = 0.28;
            canvas.Children.Add(_ghostCells[i]);

            _pieceCells[i] = CreateRect();
            canvas.Children.Add(_pieceCells[i]);
        }
    }

    public void Render(TetrisGame game)
    {
        double fade = 1.0 - game.ClearProgress;

        for (int y = 0; y < Board.Height; y++)
        {
            bool clearing = game.ClearingRows.Contains(y);
            for (int x = 0; x < Board.Width; x++)
            {
                var rect = _cells[y, x];
                var cell = game.Board[x, y];
                rect.Fill = PieceColors.For(cell);
                rect.Opacity = clearing && !cell.IsEmpty ? fade : 1.0;
            }
        }

        RenderPiece(_ghostCells, game.Ghost, ghost: true);
        RenderPiece(_pieceCells, game.Current, ghost: false);
    }

    private void RenderPiece(Rectangle[] pool, Piece? piece, bool ghost)
    {
        int index = 0;
        if (piece is not null)
        {
            var brush = PieceColors.For(piece.Type);
            foreach (var (x, y) in piece.Cells())
            {
                if (y < 0 || index >= pool.Length)
                {
                    continue;
                }

                var rect = pool[index++];
                rect.Fill = brush;
                rect.Visibility = System.Windows.Visibility.Visible;
                rect.Opacity = ghost ? 0.28 : 1.0;
                Canvas.SetLeft(rect, x * _cellSize);
                Canvas.SetTop(rect, y * _cellSize);
            }
        }

        for (; index < pool.Length; index++)
        {
            pool[index].Visibility = System.Windows.Visibility.Collapsed;
        }
    }

    private Rectangle CreateRect() => new()
    {
        Width = _cellSize,
        Height = _cellSize,
        Fill = PieceColors.Empty,
        RadiusX = Math.Max(2, _cellSize * 0.12),
        RadiusY = Math.Max(2, _cellSize * 0.12),
    };
}
