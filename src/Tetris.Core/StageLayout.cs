using System;
using System.Collections.Generic;

namespace Tetris.Core;

/// <summary>ステージ 1 面ぶんの初期配置（20 行 × 10 列）。</summary>
public sealed class StageLayout
{
    private readonly Cell[,] _cells;

    public StageLayout(int number, Cell[,] cells)
    {
        if (cells.GetLength(0) != Board.Height || cells.GetLength(1) != Board.Width)
        {
            throw new StageDataException($"ステージ {number}: 配置データは {Board.Height} 行 × {Board.Width} 列でなければなりません。");
        }

        Number = number;
        _cells = cells;
    }

    public int Number { get; }

    /// <summary>盤面座標のセル。</summary>
    public Cell this[int x, int y] => _cells[y, x];

    public int GemCount
    {
        get
        {
            int count = 0;
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
}

public sealed class StageDataException : Exception
{
    public StageDataException(string message) : base(message)
    {
    }
}

/// <summary>埋め込みステージデータの読み込み・検証。</summary>
public static class StageRepository
{
    public const string ResourceName = "Tetris.Core.Data.stages.csv";

    private static readonly Lazy<IReadOnlyList<StageLayout>> Cached = new(() => Parse(ReadEmbeddedText()));

    /// <summary>アプリに埋め込まれた全ステージ。データ不正時は <see cref="StageDataException"/>。</summary>
    public static IReadOnlyList<StageLayout> Stages => Cached.Value;

    public static string ReadEmbeddedText()
    {
        var assembly = typeof(StageRepository).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new StageDataException($"埋め込みリソース {ResourceName} が見つかりません。");
        using var reader = new System.IO.StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>ステージ CSV を解析する。`# stage N` 行で面を区切り、`0`=空 / `1`=壁 / `2`=ジェム。</summary>
    public static IReadOnlyList<StageLayout> Parse(string text)
    {
        var stages = new List<StageLayout>();
        var rows = new List<Cell[]>();
        int stageNumber = 0;

        void Flush()
        {
            if (stageNumber == 0)
            {
                return;
            }

            if (rows.Count != Board.Height)
            {
                throw new StageDataException($"ステージ {stageNumber}: 行数が {rows.Count} 行です（{Board.Height} 行必要）。");
            }

            var cells = new Cell[Board.Height, Board.Width];
            for (int y = 0; y < Board.Height; y++)
            {
                for (int x = 0; x < Board.Width; x++)
                {
                    cells[y, x] = rows[y][x];
                }
            }

            stages.Add(new StageLayout(stageNumber, cells));
            rows.Clear();
        }

        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (line.StartsWith("#", StringComparison.Ordinal))
            {
                var body = line.TrimStart('#').Trim();
                if (body.StartsWith("stage", StringComparison.OrdinalIgnoreCase))
                {
                    Flush();
                    var numberText = body.Substring("stage".Length).Trim();
                    if (!int.TryParse(numberText, out stageNumber))
                    {
                        throw new StageDataException($"ステージ見出しの番号を解釈できません: {line}");
                    }
                }

                continue;
            }

            if (stageNumber == 0)
            {
                throw new StageDataException($"`# stage N` 見出しより前に配置データがあります: {line}");
            }

            rows.Add(ParseRow(line, stageNumber, rows.Count));
        }

        Flush();

        if (stages.Count == 0)
        {
            throw new StageDataException("ステージデータが 1 面も含まれていません。");
        }

        for (int i = 0; i < stages.Count; i++)
        {
            if (stages[i].Number != i + 1)
            {
                throw new StageDataException($"ステージ番号が連番ではありません（{i + 1} 番目が {stages[i].Number}）。");
            }

            if (stages[i].GemCount == 0)
            {
                throw new StageDataException($"ステージ {stages[i].Number}: ジェムが 1 個も配置されていません。");
            }
        }

        return stages;
    }

    private static Cell[] ParseRow(string line, int stageNumber, int rowIndex)
    {
        var tokens = line.Contains(',')
            ? line.Split(',')
            : SplitChars(line);

        if (tokens.Length != Board.Width)
        {
            throw new StageDataException(
                $"ステージ {stageNumber} {rowIndex + 1} 行目: 列数が {tokens.Length} です（{Board.Width} 列必要）。");
        }

        var cells = new Cell[Board.Width];
        for (int x = 0; x < Board.Width; x++)
        {
            cells[x] = tokens[x].Trim() switch
            {
                "0" => Cell.Empty,
                "1" => Cell.Wall,
                "2" => Cell.Gem,
                var other => throw new StageDataException(
                    $"ステージ {stageNumber} {rowIndex + 1} 行目: 未知の文字 '{other}' が含まれています。"),
            };
        }

        return cells;
    }

    private static string[] SplitChars(string line)
    {
        var tokens = new string[line.Length];
        for (int i = 0; i < line.Length; i++)
        {
            tokens[i] = line[i].ToString();
        }

        return tokens;
    }
}
