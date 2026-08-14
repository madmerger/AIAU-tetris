namespace Tetris.Core;

/// <summary>アプリに埋め込んだステージ定義（CSV）の集合。</summary>
public sealed class StageSet
{
    public const string ResourceName = "Tetris.Core.Stages.csv";
    public const int ExpectedStageCount = 20;

    private static StageSet? _embedded;

    private StageSet(IReadOnlyList<StageData> stages)
    {
        Stages = stages;
    }

    public IReadOnlyList<StageData> Stages { get; }

    public int Count => Stages.Count;

    public StageData this[int number] => Stages[number - 1];

    /// <summary>埋め込みリソースから読み込む（初回のみ解析）。</summary>
    public static StageSet Embedded => _embedded ??= LoadEmbedded();

    public static StageSet LoadEmbedded()
    {
        var assembly = typeof(StageSet).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new StageDataException($"埋め込みステージデータ '{ResourceName}' が見つからない。");
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd(), ExpectedStageCount);
    }

    /// <summary>CSV テキストを解析する。<paramref name="expectedCount"/> が指定されると面数も検証する。</summary>
    public static StageSet Parse(string text, int? expectedCount = null)
    {
        ArgumentNullException.ThrowIfNull(text);

        var stages = new List<StageData>();
        var rows = new List<string>();

        void Flush()
        {
            if (rows.Count == 0)
            {
                return;
            }

            stages.Add(StageData.Parse(stages.Count + 1, rows.ToArray()));
            rows.Clear();
        }

        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim('\r', ' ', '\t');
            if (line.Length == 0 || line.StartsWith('#'))
            {
                Flush();
                continue;
            }

            rows.Add(line.Replace(",", string.Empty));
        }

        Flush();

        if (expectedCount is { } expected && stages.Count != expected)
        {
            throw new StageDataException($"ステージ数が {expected} ではなく {stages.Count} 面。");
        }

        if (stages.Count == 0)
        {
            throw new StageDataException("ステージデータが空。");
        }

        return new StageSet(stages);
    }
}
