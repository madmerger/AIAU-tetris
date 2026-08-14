using System;
using System.Collections.Generic;
using System.Linq;

namespace Tetris.Core;

/// <summary>10 列 × 20 行の盤面。</summary>
public sealed class Board
{
    public const int Width = 10;
    public const int Height = 20;

    private readonly Cell[,] _cells = new Cell[Height, Width];

    public Board()
    {
        Clear();
    }

    public Cell this[int x, int y]
    {
        get => IsInside(x, y) ? _cells[y, x] : Cell.Wall;
        set
        {
            if (!IsInside(x, y))
            {
                throw new ArgumentOutOfRangeException(nameof(x), $"({x},{y}) は盤面外です。");
            }

            _cells[y, x] = value;
        }
    }

    public void Clear()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                _cells[y, x] = Cell.Empty;
            }
        }
    }

    public static bool IsInside(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

    /// <summary>
    /// ピースを現在位置に置けるか（左右の壁・床・既存ブロックとの衝突が無いか）。
    /// 盤面上端より上（y &lt; 0）は空きとして扱う。
    /// </summary>
    public bool CanPlace(Piece piece)
    {
        foreach (var (x, y) in piece.Cells())
        {
            if (x < 0 || x >= Width || y >= Height)
            {
                return false;
            }

            if (y >= 0 && !_cells[y, x].IsEmpty)
            {
                return false;
            }
        }

        return true;
    }

    public void Lock(Piece piece)
    {
        foreach (var (x, y) in piece.Cells())
        {
            if (IsInside(x, y))
            {
                _cells[y, x] = Cell.Block(piece.Type);
            }
        }
    }

    /// <summary>横 1 列が埋まっている行を上から順に返す。</summary>
    public List<int> FindFullRows()
    {
        var rows = new List<int>();
        for (int y = 0; y < Height; y++)
        {
            bool full = true;
            for (int x = 0; x < Width; x++)
            {
                if (_cells[y, x].IsEmpty)
                {
                    full = false;
                    break;
                }
            }

            if (full)
            {
                rows.Add(y);
            }
        }

        return rows;
    }

    /// <summary>指定行を消去し、上の行を下へ詰める。</summary>
    public void ClearRows(IEnumerable<int> rows)
    {
        var target = new HashSet<int>(rows);
        if (target.Count == 0)
        {
            return;
        }

        int writeY = Height - 1;
        for (int readY = Height - 1; readY >= 0; readY--)
        {
            if (target.Contains(readY))
            {
                continue;
            }

            if (writeY != readY)
            {
                for (int x = 0; x < Width; x++)
                {
                    _cells[writeY, x] = _cells[readY, x];
                }
            }

            writeY--;
        }

        for (; writeY >= 0; writeY--)
        {
            for (int x = 0; x < Width; x++)
            {
                _cells[writeY, x] = Cell.Empty;
            }
        }
    }

    public int CountGems()
    {
        int count = 0;
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (_cells[y, x].Kind == CellKind.Gem)
                {
                    count++;
                }
            }
        }

        return count;
    }

    /// <summary>ステージ初期配置を読み込む。</summary>
    public void LoadLayout(StageLayout layout)
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                _cells[y, x] = layout[x, y];
            }
        }
    }

    public IEnumerable<Cell> AllCells()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                yield return _cells[y, x];
            }
        }
    }

    public override string ToString() =>
        string.Join(
            Environment.NewLine,
            Enumerable.Range(0, Height).Select(y =>
                string.Concat(Enumerable.Range(0, Width).Select(x => _cells[y, x].Kind switch
                {
                    CellKind.Empty => ".",
                    CellKind.Wall => "#",
                    CellKind.Gem => "*",
                    _ => "X",
                }))));
}
