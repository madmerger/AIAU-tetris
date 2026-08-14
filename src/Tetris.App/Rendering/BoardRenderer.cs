using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Tetris.Core;

namespace Tetris.App.Rendering;

/// <summary>盤面・ゴースト・現在ピース・演出を毎フレーム直接描画する要素（要素の生成/破棄を伴わない）。</summary>
public sealed class BoardRenderer : FrameworkElement
{
    private static readonly Typeface Typeface = new("Consolas");
    private readonly Pen _gridPen = CreatePen(BlockTheme.GridLine, 0.6);
    private readonly Pen _borderPen = CreatePen(BlockTheme.BoardBorder, 2.0);
    private readonly Pen _ghostPen = CreatePen(BlockTheme.GhostFill, 1.5);

    public TetrisGame? Game { get; set; }

    protected override void OnRender(DrawingContext dc)
    {
        var size = RenderSize;
        dc.DrawRectangle(BlockTheme.Background, null, new Rect(0, 0, size.Width, size.Height));

        var game = Game;
        if (game is null)
        {
            return;
        }

        var board = game.Board;
        var cell = Math.Min(size.Width / board.Width, size.Height / board.Height);
        var originX = (size.Width - (cell * board.Width)) / 2;
        var originY = (size.Height - (cell * board.Height)) / 2;

        DrawGrid(dc, board, cell, originX, originY);
        DrawLockedCells(dc, game, board, cell, originX, originY);

        if (game.Phase is GamePhase.Playing or GamePhase.Paused)
        {
            DrawGhost(dc, game, cell, originX, originY);
            DrawPiece(dc, game.Current, cell, originX, originY, 1.0);
        }

        dc.DrawRectangle(
            null,
            _borderPen,
            new Rect(originX, originY, cell * board.Width, cell * board.Height));

        DrawOverlay(dc, game, size);
    }

    private void DrawGrid(DrawingContext dc, Board board, double cell, double originX, double originY)
    {
        for (var x = 0; x <= board.Width; x++)
        {
            var px = originX + (x * cell);
            dc.DrawLine(_gridPen, new Point(px, originY), new Point(px, originY + (cell * board.Height)));
        }

        for (var y = 0; y <= board.Height; y++)
        {
            var py = originY + (y * cell);
            dc.DrawLine(_gridPen, new Point(originX, py), new Point(originX + (cell * board.Width), py));
        }
    }

    private static void DrawLockedCells(
        DrawingContext dc,
        TetrisGame game,
        Board board,
        double cell,
        double originX,
        double originY)
    {
        // 消去対象行はフェードアウト（約 0.3 秒）させる。
        var fade = 1.0 - game.ClearProgress;
        for (var y = 0; y < board.Height; y++)
        {
            var opacity = game.ClearingRows.Contains(y) ? fade : 1.0;
            for (var x = 0; x < board.Width; x++)
            {
                var content = board[x, y];
                if (!content.IsOccupied)
                {
                    continue;
                }

                DrawBlock(dc, BlockTheme.ForCell(content), originX + (x * cell), originY + (y * cell), cell, opacity);
                if (content.Kind == CellKind.Gem)
                {
                    DrawGemMarker(dc, originX + (x * cell), originY + (y * cell), cell, opacity);
                }
            }
        }
    }

    private void DrawGhost(DrawingContext dc, TetrisGame game, double cell, double originX, double originY)
    {
        var ghost = game.Current with { Y = game.GhostY };
        if (ghost.Y == game.Current.Y)
        {
            return;
        }

        foreach (var block in ghost.BoardCells())
        {
            var rect = new Rect(
                originX + (block.X * cell) + 2,
                originY + (block.Y * cell) + 2,
                Math.Max(cell - 4, 1),
                Math.Max(cell - 4, 1));
            dc.DrawRectangle(null, _ghostPen, rect);
        }
    }

    private static void DrawPiece(
        DrawingContext dc,
        Piece piece,
        double cell,
        double originX,
        double originY,
        double opacity)
    {
        var brush = BlockTheme.ForPiece(piece.Type);
        foreach (var block in piece.BoardCells())
        {
            if (block.Y < 0)
            {
                continue;
            }

            DrawBlock(dc, brush, originX + (block.X * cell), originY + (block.Y * cell), cell, opacity);
        }
    }

    private static void DrawBlock(DrawingContext dc, Brush brush, double x, double y, double cell, double opacity)
    {
        if (opacity < 1.0)
        {
            dc.PushOpacity(Math.Clamp(opacity, 0.0, 1.0));
        }

        var inset = Math.Max(cell * 0.06, 0.5);
        dc.DrawRectangle(brush, null, new Rect(x + inset, y + inset, cell - (inset * 2), cell - (inset * 2)));

        if (opacity < 1.0)
        {
            dc.Pop();
        }
    }

    private static void DrawGemMarker(DrawingContext dc, double x, double y, double cell, double opacity)
    {
        if (opacity < 1.0)
        {
            dc.PushOpacity(Math.Clamp(opacity, 0.0, 1.0));
        }

        var center = new Point(x + (cell / 2), y + (cell / 2));
        dc.DrawEllipse(BlockTheme.Text, null, center, cell * 0.16, cell * 0.16);

        if (opacity < 1.0)
        {
            dc.Pop();
        }
    }

    private void DrawOverlay(DrawingContext dc, TetrisGame game, Size size)
    {
        var (title, subtitle) = game.Phase switch
        {
            GamePhase.Paused => ("PAUSE", "P キーで再開"),
            GamePhase.GameOver => ("GAME OVER", "リスタート または モード選択へ"),
            GamePhase.StageClear => ("GAME CLEAR", $"次のステージまで {Math.Ceiling(game.StageClearRemaining):0}"),
            GamePhase.AllClear => ("ALL CLEAR", $"全 {game.StageCount} ステージ制覇"),
            _ => (null, null),
        };

        if (title is null)
        {
            return;
        }

        dc.DrawRectangle(BlockTheme.OverlayScrim, null, new Rect(0, 0, size.Width, size.Height));
        DrawCenteredText(dc, title, 34, BlockTheme.Accent, size, -20);
        if (subtitle is not null)
        {
            DrawCenteredText(dc, subtitle, 14, BlockTheme.Text, size, 28);
        }
    }

    private void DrawCenteredText(
        DrawingContext dc,
        string text,
        double fontSize,
        Brush brush,
        Size size,
        double offsetY)
    {
        var formatted = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            Typeface,
            fontSize,
            brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        dc.DrawText(
            formatted,
            new Point((size.Width - formatted.Width) / 2, ((size.Height - formatted.Height) / 2) + offsetY));
    }

    private static Pen CreatePen(Brush brush, double thickness)
    {
        var pen = new Pen(brush, thickness);
        pen.Freeze();
        return pen;
    }
}
