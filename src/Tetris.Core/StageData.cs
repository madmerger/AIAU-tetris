namespace Tetris.Core;

/// <summary>ステージデータの不正を表す例外。</summary>
public sealed class StageDataException : Exception
{
    public StageDataException(string message)
        : base(message)
    {
    }
}

/// <summary>1 ステージ分の初期配置（20 行 × 10 列、<c>0</c>=空 / <c>1</c>=壁 / <c>2</c>=ジェム）。</summary>
public sealed class StageData
{
    private readonly Cell[] _cells;

    private StageData(int number, int width, int height, Cell[] cells)
    {
        Number = number;
        Width = width;
        Height = height;
        _cells = cells;
    }

    public int Number { get; }

    public int Width { get; }

    public int Height { get; }

    public Cell this[int x, int y] => _cells[(y * Width) + x];

    public int GemCount
    {
        get
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
    }

    /// <summary>行データを検証して読み込む。行数・列数不一致や未知の文字は <see cref="StageDataException"/>。</summary>
    public static StageData Parse(
        int number,
        IReadOnlyList<string> rows,
        int width = Board.DefaultWidth,
        int height = Board.DefaultHeight)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (rows.Count != height)
        {
            throw new StageDataException($"ステージ {number}: 行数が {height} ではなく {rows.Count} 行。");
        }

        var cells = new Cell[width * height];
        for (var y = 0; y < height; y++)
        {
            var row = rows[y];
            if (row.Length != width)
            {
                throw new StageDataException($"ステージ {number}: {y + 1} 行目の列数が {width} ではなく {row.Length} 列。");
            }

            for (var x = 0; x < width; x++)
            {
                cells[(y * width) + x] = row[x] switch
                {
                    '0' => Cell.Empty,
                    '1' => Cell.Wall,
                    '2' => Cell.Gem,
                    var c => throw new StageDataException($"ステージ {number}: {y + 1} 行 {x + 1} 列に未知の文字 '{c}'。"),
                };
            }
        }

        var stage = new StageData(number, width, height, cells);
        if (stage.GemCount == 0)
        {
            throw new StageDataException($"ステージ {number}: ジェムが 1 個も配置されていない。");
        }

        return stage;
    }
}
