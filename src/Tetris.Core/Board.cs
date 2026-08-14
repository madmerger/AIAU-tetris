namespace Tetris.Core;

/// <summary>10 列 × 20 行の盤面。UI 非依存。</summary>
public sealed class Board
{
    public const int DefaultWidth = 10;
    public const int DefaultHeight = 20;

    private readonly Cell[] _cells;

    public Board(int width = DefaultWidth, int height = DefaultHeight)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "盤面サイズは正の値でなければならない。");
        }

        Width = width;
        Height = height;
        _cells = new Cell[width * height];
    }

    public int Width { get; }

    public int Height { get; }

    public Cell this[int x, int y]
    {
        get => IsInside(x, y) ? _cells[(y * Width) + x] : throw new ArgumentOutOfRangeException(nameof(x));
        set
        {
            if (!IsInside(x, y))
            {
                throw new ArgumentOutOfRangeException(nameof(x));
            }

            _cells[(y * Width) + x] = value;
        }
    }

    public bool IsInside(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

    public bool IsOccupied(int x, int y) => IsInside(x, y) && _cells[(y * Width) + x].IsOccupied;

    public void Clear() => Array.Fill(_cells, Cell.Empty);

    /// <summary>ピースが盤内に収まり、既存ブロックと重ならないか。</summary>
    public bool CanPlace(in Piece piece)
    {
        foreach (var cell in piece.BoardCells())
        {
            if (!IsInside(cell.X, cell.Y) || IsOccupied(cell.X, cell.Y))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>ピースを盤面へ固定する。</summary>
    public void Lock(in Piece piece)
    {
        foreach (var cell in piece.BoardCells())
        {
            if (!IsInside(cell.X, cell.Y))
            {
                throw new InvalidOperationException("盤外にピースを固定できない。");
            }

            this[cell.X, cell.Y] = Cell.OfPiece(piece.Type);
        }
    }

    /// <summary>ハードドロップ／ゴースト位置（これ以上落下できない Y）。</summary>
    public int DropY(in Piece piece)
    {
        var y = piece.Y;
        while (CanPlace(piece with { Y = y + 1 }))
        {
            y++;
        }

        return y;
    }

    /// <summary>横一列が埋まっている行を上から順に返す。</summary>
    public List<int> FindFullRows()
    {
        var rows = new List<int>();
        for (var y = 0; y < Height; y++)
        {
            var full = true;
            for (var x = 0; x < Width; x++)
            {
                if (!IsOccupied(x, y))
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
    public void ClearRows(IReadOnlyCollection<int> rows)
    {
        if (rows.Count == 0)
        {
            return;
        }

        var removed = new HashSet<int>(rows);
        var writeY = Height - 1;
        for (var readY = Height - 1; readY >= 0; readY--)
        {
            if (removed.Contains(readY))
            {
                continue;
            }

            if (writeY != readY)
            {
                for (var x = 0; x < Width; x++)
                {
                    this[x, writeY] = this[x, readY];
                }
            }

            writeY--;
        }

        for (; writeY >= 0; writeY--)
        {
            for (var x = 0; x < Width; x++)
            {
                this[x, writeY] = Cell.Empty;
            }
        }
    }

    public int CountGems()
    {
        var count = 0;
        foreach (var cell in _cells)
        {
            if (cell.Kind == CellKind.Gem)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>ステージ初期配置を読み込む（盤面はクリアされる）。</summary>
    public void LoadStage(StageData stage)
    {
        ArgumentNullException.ThrowIfNull(stage);
        if (stage.Width != Width || stage.Height != Height)
        {
            throw new ArgumentException("ステージデータのサイズが盤面と一致しない。", nameof(stage));
        }

        Clear();
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                this[x, y] = stage[x, y];
            }
        }
    }
}
