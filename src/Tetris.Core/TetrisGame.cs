using System;
using System.Collections.Generic;
using System.Linq;

namespace Tetris.Core;

public enum GameMode
{
    Infinite,
    Stage,
}

public enum GamePhase
{
    /// <summary>通常プレイ中。</summary>
    Playing,

    /// <summary>ライン消去のフェードアウト演出中（入力は破棄）。</summary>
    Clearing,

    /// <summary>ポーズ中（入力は破棄）。</summary>
    Paused,

    /// <summary>ステージクリア演出＋カウントダウン中。</summary>
    StageClear,

    /// <summary>全ステージクリア。</summary>
    AllClear,

    GameOver,
}

public enum GameSound
{
    GameStart,
    PieceLock,
    StageClear,
}

/// <summary>
/// 実時間から切り離したテトリスのゲームロジック。<see cref="Update"/> に経過時間を与えて進行させる。
/// </summary>
public sealed class TetrisGame
{
    public static readonly TimeSpan InitialFallInterval = TimeSpan.FromSeconds(0.8);
    public static readonly TimeSpan MinimumFallInterval = TimeSpan.FromMilliseconds(50);
    public static readonly TimeSpan LineClearAnimation = TimeSpan.FromSeconds(0.3);
    public static readonly TimeSpan StageClearCountdown = TimeSpan.FromSeconds(3);
    public const int LinesPerLevel = 20;
    public const double SoftDropMultiplier = 10.0;
    private static readonly int[] LineScores = { 0, 40, 100, 300, 1200 };

    private readonly IReadOnlyList<StageLayout> _stages;
    private readonly int? _seed;
    private readonly List<GameSound> _sounds = new();
    private readonly List<int> _clearingRows = new();

    private readonly IPieceSource? _injectedSource;

    private IPieceSource _bag = new PieceBag();
    private TimeSpan _fallTimer;
    private TimeSpan _clearTimer;
    private TimeSpan _countdownTimer;
    private GamePhase _pausedFrom;

    /// <param name="pieceSource">出現順の供給元。未指定の場合は 7 種バッグを使う。</param>
    public TetrisGame(
        GameMode mode,
        int? seed = null,
        IReadOnlyList<StageLayout>? stages = null,
        IPieceSource? pieceSource = null)
    {
        Mode = mode;
        _seed = seed;
        _injectedSource = pieceSource;
        _stages = stages ?? (mode == GameMode.Stage ? StageRepository.Stages : Array.Empty<StageLayout>());
        if (mode == GameMode.Stage && _stages.Count == 0)
        {
            throw new StageDataException("ステージモードにはステージデータが必要です。");
        }

        StartStage(1);
    }

    public GameMode Mode { get; }

    public Board Board { get; } = new();

    public GamePhase Phase { get; private set; }

    public Piece? Current { get; private set; }

    public PieceType NextPiece { get; private set; }

    public int Score { get; private set; }

    public int Level { get; private set; }

    /// <summary>現在のステージ（インフィニティモードではゲーム全体）での累計消去ライン数。</summary>
    public int LinesCleared { get; private set; }

    public int StageNumber { get; private set; }

    public int StageCount => _stages.Count;

    public TimeSpan Elapsed { get; private set; }

    public bool SoftDropping { get; set; }

    /// <summary>消去演出中の対象行。</summary>
    public IReadOnlyList<int> ClearingRows => _clearingRows;

    /// <summary>消去演出の進捗 0.0〜1.0（フェードアウト用）。</summary>
    public double ClearProgress =>
        Phase == GamePhase.Clearing
            ? Math.Clamp(_clearTimer.TotalSeconds / LineClearAnimation.TotalSeconds, 0, 1)
            : 0;

    /// <summary>ステージクリア演出の残りカウント秒（3, 2, 1）。</summary>
    public int CountdownSeconds => (int)Math.Ceiling(Math.Max(_countdownTimer.TotalSeconds, 0));

    public bool IsActive => Phase is GamePhase.Playing or GamePhase.Clearing;

    public TimeSpan FallInterval => ComputeFallInterval(Level);

    /// <summary>レベルごとの落下間隔。レベル +1 ごとに約 10% 短縮し、下限 50ms。</summary>
    public static TimeSpan ComputeFallInterval(int level)
    {
        double seconds = InitialFallInterval.TotalSeconds * Math.Pow(0.9, Math.Max(level, 1) - 1);
        var interval = TimeSpan.FromSeconds(seconds);
        return interval < MinimumFallInterval ? MinimumFallInterval : interval;
    }

    /// <summary>現在ピースの着地位置（ゴーストピース）。</summary>
    public Piece? Ghost
    {
        get
        {
            if (Current is null)
            {
                return null;
            }

            var ghost = new Piece(Current.Type, Current.X, Current.Y, Current.Rotation);
            while (Board.CanPlace(ghost.Moved(0, 1)))
            {
                ghost = ghost.Moved(0, 1);
            }

            return ghost;
        }
    }

    public IReadOnlyList<GameSound> DrainSounds()
    {
        var drained = _sounds.ToArray();
        _sounds.Clear();
        return drained;
    }

    /// <summary>現在のステージ（インフィニティモードでは最初から）をやり直す。</summary>
    public void Restart() => StartStage(Mode == GameMode.Stage ? StageNumber : 1);

    /// <summary>モード選択画面へ戻らずに 1 面目から遊び直す。</summary>
    public void RestartFromFirstStage() => StartStage(1);

    public void TogglePause()
    {
        if (Phase is GamePhase.Playing or GamePhase.Clearing)
        {
            _pausedFrom = Phase;
            Phase = GamePhase.Paused;
        }
        else if (Phase == GamePhase.Paused)
        {
            Phase = _pausedFrom;
        }
    }

    public bool MoveLeft() => TryMove(-1, 0);

    public bool MoveRight() => TryMove(1, 0);

    public bool MoveDown() => TryMove(0, 1);

    public bool RotateClockwise()
    {
        if (Phase != GamePhase.Playing || Current is null)
        {
            return false;
        }

        var rotated = Current.Rotated(1);
        if (!Board.CanPlace(rotated))
        {
            return false;
        }

        Current.ApplyFrom(rotated);
        return true;
    }

    /// <summary>ゴースト位置へ即着地させ、猶予なくその場で固定する。</summary>
    public void HardDrop()
    {
        if (Phase != GamePhase.Playing || Current is null)
        {
            return;
        }

        int distance = 0;
        while (Board.CanPlace(Current.Moved(0, 1)))
        {
            Current.ApplyFrom(Current.Moved(0, 1));
            distance++;
        }

        Score += distance;
        LockPiece();
    }

    public void Update(TimeSpan delta)
    {
        if (delta <= TimeSpan.Zero)
        {
            return;
        }

        switch (Phase)
        {
            case GamePhase.Playing:
                Elapsed += delta;
                UpdateFalling(delta);
                break;
            case GamePhase.Clearing:
                Elapsed += delta;
                _clearTimer += delta;
                if (_clearTimer >= LineClearAnimation)
                {
                    CompleteLineClear();
                }

                break;
            case GamePhase.StageClear:
                _countdownTimer -= delta;
                if (_countdownTimer <= TimeSpan.Zero)
                {
                    AdvanceStage();
                }

                break;
        }
    }

    private void UpdateFalling(TimeSpan delta)
    {
        var interval = SoftDropping
            ? TimeSpan.FromSeconds(FallInterval.TotalSeconds / SoftDropMultiplier)
            : FallInterval;

        _fallTimer += delta;
        while (Phase == GamePhase.Playing && _fallTimer >= interval)
        {
            _fallTimer -= interval;
            if (Current is null)
            {
                break;
            }

            if (Board.CanPlace(Current.Moved(0, 1)))
            {
                Current.ApplyFrom(Current.Moved(0, 1));
            }
            else
            {
                LockPiece();
            }
        }
    }

    private bool TryMove(int dx, int dy)
    {
        if (Phase != GamePhase.Playing || Current is null)
        {
            return false;
        }

        var moved = Current.Moved(dx, dy);
        if (!Board.CanPlace(moved))
        {
            return false;
        }

        Current.ApplyFrom(moved);
        return true;
    }

    private void StartStage(int stageNumber)
    {
        StageNumber = stageNumber;
        Board.Clear();
        if (Mode == GameMode.Stage)
        {
            Board.LoadLayout(_stages[stageNumber - 1]);
        }

        Score = 0;
        Level = 1;
        LinesCleared = 0;
        Elapsed = TimeSpan.Zero;
        _fallTimer = TimeSpan.Zero;
        _clearTimer = TimeSpan.Zero;
        _countdownTimer = TimeSpan.Zero;
        _clearingRows.Clear();
        SoftDropping = false;
        _bag = _injectedSource ?? (_seed.HasValue ? new PieceBag(_seed.Value + stageNumber) : new PieceBag());
        NextPiece = _bag.Next();
        Phase = GamePhase.Playing;
        _sounds.Add(GameSound.GameStart);
        SpawnNext();
    }

    private void SpawnNext()
    {
        var type = NextPiece;
        NextPiece = _bag.Next();
        Current = CreateSpawnPiece(type);
        _fallTimer = TimeSpan.Zero;
        if (!Board.CanPlace(Current))
        {
            Phase = GamePhase.GameOver;
        }
    }

    /// <summary>ピースの出現位置（上端が 0 行目、水平中央寄せ）。</summary>
    public static Piece CreateSpawnPiece(PieceType type)
    {
        var shape = Tetromino.Shape(type, 0);
        int size = shape.GetLength(0);
        int topOffset = 0;
        for (int r = 0; r < size; r++)
        {
            bool any = false;
            for (int c = 0; c < size; c++)
            {
                any |= shape[r, c];
            }

            if (any)
            {
                topOffset = r;
                break;
            }
        }

        int x = (Board.Width - size) / 2;
        return new Piece(type, x, -topOffset);
    }

    private void LockPiece()
    {
        if (Current is null)
        {
            return;
        }

        Board.Lock(Current);
        Current = null;
        SoftDropping = false;
        _sounds.Add(GameSound.PieceLock);

        var fullRows = Board.FindFullRows();
        if (fullRows.Count > 0)
        {
            _clearingRows.Clear();
            _clearingRows.AddRange(fullRows);
            _clearTimer = TimeSpan.Zero;
            Phase = GamePhase.Clearing;
            return;
        }

        SpawnNext();
    }

    private void CompleteLineClear()
    {
        int count = _clearingRows.Count;
        Board.ClearRows(_clearingRows);
        _clearingRows.Clear();
        _clearTimer = TimeSpan.Zero;

        Score += LineScores[Math.Min(count, 4)] * Level;
        LinesCleared += count;
        Level = 1 + (LinesCleared / LinesPerLevel);
        Phase = GamePhase.Playing;

        if (Mode == GameMode.Stage && Board.CountGems() == 0)
        {
            _sounds.Add(GameSound.StageClear);
            _countdownTimer = StageClearCountdown;
            Phase = GamePhase.StageClear;
            return;
        }

        SpawnNext();
    }

    private void AdvanceStage()
    {
        if (StageNumber >= _stages.Count)
        {
            Phase = GamePhase.AllClear;
            Current = null;
            return;
        }

        StartStage(StageNumber + 1);
    }
}
