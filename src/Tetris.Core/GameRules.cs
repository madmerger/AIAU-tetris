namespace Tetris.Core;

public enum GameMode
{
    Infinite,
    Stage,
}

public enum GamePhase
{
    /// <summary>プレイ中。</summary>
    Playing,

    /// <summary>ポーズ中。</summary>
    Paused,

    /// <summary>ライン消去のフェードアウト演出中。</summary>
    LineClearing,

    /// <summary>ステージクリア演出（カウントダウン）中。</summary>
    StageClear,

    /// <summary>全ステージクリア。</summary>
    AllClear,

    /// <summary>ゲームオーバー。</summary>
    GameOver,
}

public enum GameEventType
{
    PieceLocked,
    LinesCleared,
    LevelUp,
    StageCleared,
    AllCleared,

    /// <summary>ALL CLEAR 表示が終わり、モード選択へ戻るべき。</summary>
    AllClearFinished,
    GameOver,
}

/// <summary>仕様書で定義された数値ルール。</summary>
public static class GameRules
{
    public const double InitialFallInterval = 0.8;
    public const double FallIntervalFactorPerLevel = 0.9;
    public const double MinimumFallInterval = 0.05;
    public const double SoftDropMultiplier = 10.0;
    public const int LinesPerLevel = 20;
    public const double LineClearAnimationSeconds = 0.3;
    public const double StageClearCountdownSeconds = 3.0;
    public const double AllClearDisplaySeconds = 5.0;
    public const int HardDropPointsPerCell = 1;

    private static readonly int[] LineScores = { 0, 40, 100, 300, 1200 };

    /// <summary>同時消去行数とレベルからスコア加算量を求める。</summary>
    public static int LineClearScore(int lines, int level)
    {
        if (lines < 0 || lines >= LineScores.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(lines), lines, "同時消去は 0〜4 行。");
        }

        return LineScores[lines] * level;
    }

    /// <summary>レベルに応じた自動落下間隔（秒）。</summary>
    public static double FallInterval(int level)
    {
        if (level < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(level), level, "レベルは 1 以上。");
        }

        var interval = InitialFallInterval * Math.Pow(FallIntervalFactorPerLevel, level - 1);
        return Math.Max(interval, MinimumFallInterval);
    }

    /// <summary>累計消去ライン数から到達レベルを求める。</summary>
    public static int LevelForLines(int totalLines) => 1 + (Math.Max(totalLines, 0) / LinesPerLevel);
}
