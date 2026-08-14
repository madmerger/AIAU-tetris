namespace Tetris.Core;

/// <summary>実時間から切り離したテトリスのゲームロジック。<see cref="Update"/> に経過秒を与えて進行させる。</summary>
public sealed class TetrisGame
{
    private readonly IPieceGenerator _generator;
    private readonly IReadOnlyList<StageData> _stages;
    private readonly List<GameEventType> _events = new();
    private readonly List<int> _clearingRows = new();

    private double _fallTimer;
    private double _clearTimer;

    public TetrisGame(GameMode mode, IPieceGenerator generator, IReadOnlyList<StageData>? stages = null)
    {
        ArgumentNullException.ThrowIfNull(generator);
        Mode = mode;
        _generator = generator;
        _stages = stages ?? (mode == GameMode.Stage ? StageSet.Embedded.Stages : Array.Empty<StageData>());
        if (mode == GameMode.Stage && _stages.Count == 0)
        {
            throw new ArgumentException("STAGE MODE ではステージデータが必要。", nameof(stages));
        }

        Board = new Board();
        StartStage(1);
    }

    public GameMode Mode { get; }

    public Board Board { get; }

    public GamePhase Phase { get; private set; }

    public Piece Current { get; private set; }

    public PieceType NextPiece { get; private set; }

    public int Score { get; private set; }

    public int Level { get; private set; }

    public int TotalLines { get; private set; }

    public int StageNumber { get; private set; }

    public int StageCount => _stages.Count;

    public double ElapsedSeconds { get; private set; }

    /// <summary>ソフトドロップ（↓ 長押し）中か。</summary>
    public bool SoftDropping { get; set; }

    /// <summary>フェードアウト演出中の消去対象行。</summary>
    public IReadOnlyList<int> ClearingRows => _clearingRows;

    /// <summary>ライン消去演出の進行度（0.0〜1.0）。</summary>
    public double ClearProgress => _clearingRows.Count == 0
        ? 0.0
        : Math.Clamp(_clearTimer / GameRules.LineClearAnimationSeconds, 0.0, 1.0);

    /// <summary>ステージクリア演出の残りカウントダウン秒。</summary>
    public double StageClearRemaining { get; private set; }

    /// <summary>ALL CLEAR 表示の残り秒（0 になるとモード選択へ戻る）。</summary>
    public double AllClearRemaining { get; private set; }

    /// <summary>盤面上に残っているジェム数（STAGE MODE のクリア条件）。</summary>
    public int RemainingGems => Board.CountGems();

    public bool IsFinished => Phase is GamePhase.GameOver or GamePhase.AllClear;

    /// <summary>現在ピースのゴースト（着地）位置の Y。</summary>
    public int GhostY => Board.DropY(Current);

    /// <summary>発生したイベントを取り出す（効果音再生用）。</summary>
    public IReadOnlyList<GameEventType> DrainEvents()
    {
        var drained = _events.ToArray();
        _events.Clear();
        return drained;
    }

    /// <summary>現在ステージを最初からやり直す。</summary>
    public void Restart() => StartStage(Mode == GameMode.Stage ? StageNumber : 1);

    public void Update(double deltaSeconds)
    {
        if (deltaSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
        }

        if (Phase is GamePhase.Paused or GamePhase.GameOver)
        {
            return;
        }

        if (Phase == GamePhase.AllClear)
        {
            CountDownAllClear(deltaSeconds);
            return;
        }

        ElapsedSeconds += deltaSeconds;

        switch (Phase)
        {
            case GamePhase.LineClearing:
                _clearTimer += deltaSeconds;
                if (_clearTimer >= GameRules.LineClearAnimationSeconds)
                {
                    CompleteLineClear();
                }

                break;

            case GamePhase.StageClear:
                StageClearRemaining -= deltaSeconds;
                if (StageClearRemaining <= 0)
                {
                    AdvanceStage();
                }

                break;

            case GamePhase.Playing:
                AdvanceFall(deltaSeconds);
                break;
        }
    }

    public bool MoveLeft() => TryMove(-1, 0);

    public bool MoveRight() => TryMove(1, 0);

    public bool RotateClockwise()
    {
        if (Phase != GamePhase.Playing)
        {
            return false;
        }

        var rotated = Current.RotatedClockwise();
        if (!Board.CanPlace(rotated))
        {
            return false;
        }

        Current = rotated;
        return true;
    }

    /// <summary>ゴースト位置へ即着地し、猶予なくその場で固定する。</summary>
    public bool HardDrop()
    {
        if (Phase != GamePhase.Playing)
        {
            return false;
        }

        var target = Board.DropY(Current);
        var distance = target - Current.Y;
        Current = Current with { Y = target };
        Score += distance * GameRules.HardDropPointsPerCell;
        LockPiece();
        return true;
    }

    /// <summary>ポーズ / 再開を切り替える。</summary>
    public void TogglePause()
    {
        Phase = Phase switch
        {
            GamePhase.Playing => GamePhase.Paused,
            GamePhase.Paused => GamePhase.Playing,
            _ => Phase,
        };
    }

    private void StartStage(int stageNumber)
    {
        Score = 0;
        Level = 1;
        TotalLines = 0;
        ElapsedSeconds = 0;
        SoftDropping = false;
        _fallTimer = 0;
        _clearTimer = 0;
        _clearingRows.Clear();
        StageClearRemaining = 0;
        AllClearRemaining = 0;
        _events.Clear();
        StageNumber = Mode == GameMode.Stage ? stageNumber : 0;

        Board.Clear();
        if (Mode == GameMode.Stage)
        {
            Board.LoadStage(_stages[stageNumber - 1]);
        }

        Phase = GamePhase.Playing;
        NextPiece = _generator.Next();
        SpawnNext();
    }

    private void AdvanceStage()
    {
        if (Mode != GameMode.Stage)
        {
            return;
        }

        if (StageNumber >= _stages.Count)
        {
            Phase = GamePhase.AllClear;
            AllClearRemaining = GameRules.AllClearDisplaySeconds;
            _events.Add(GameEventType.AllCleared);
            return;
        }

        StartStage(StageNumber + 1);
    }

    private void CountDownAllClear(double deltaSeconds)
    {
        if (AllClearRemaining <= 0)
        {
            return;
        }

        AllClearRemaining -= deltaSeconds;
        if (AllClearRemaining <= 0)
        {
            AllClearRemaining = 0;
            _events.Add(GameEventType.AllClearFinished);
        }
    }

    private void AdvanceFall(double deltaSeconds)
    {
        var interval = GameRules.FallInterval(Level);
        if (SoftDropping)
        {
            interval /= GameRules.SoftDropMultiplier;
        }

        _fallTimer += deltaSeconds;
        while (Phase == GamePhase.Playing && _fallTimer >= interval)
        {
            _fallTimer -= interval;
            StepDown();
        }
    }

    private void StepDown()
    {
        var moved = Current.Moved(0, 1);
        if (Board.CanPlace(moved))
        {
            Current = moved;
            return;
        }

        LockPiece();
    }

    private bool TryMove(int dx, int dy)
    {
        if (Phase != GamePhase.Playing)
        {
            return false;
        }

        var moved = Current.Moved(dx, dy);
        if (!Board.CanPlace(moved))
        {
            return false;
        }

        Current = moved;
        return true;
    }

    private void LockPiece()
    {
        Board.Lock(Current);
        _events.Add(GameEventType.PieceLocked);
        _fallTimer = 0;

        var fullRows = Board.FindFullRows();
        if (fullRows.Count == 0)
        {
            SpawnNext();
            return;
        }

        // 不正なステージデータで 5 行以上同時に揃った場合もスコア計算で落ちないようにする。
        Score += GameRules.LineClearScore(Math.Min(fullRows.Count, 4), Level);
        TotalLines += fullRows.Count;

        var newLevel = GameRules.LevelForLines(TotalLines);
        if (newLevel != Level)
        {
            Level = newLevel;
            _events.Add(GameEventType.LevelUp);
        }

        _clearingRows.Clear();
        _clearingRows.AddRange(fullRows);
        _clearTimer = 0;
        Phase = GamePhase.LineClearing;
        _events.Add(GameEventType.LinesCleared);
    }

    private void CompleteLineClear()
    {
        Board.ClearRows(_clearingRows);
        _clearingRows.Clear();
        _clearTimer = 0;
        Phase = GamePhase.Playing;

        if (Mode == GameMode.Stage && Board.CountGems() == 0)
        {
            Phase = GamePhase.StageClear;
            StageClearRemaining = GameRules.StageClearCountdownSeconds;
            _events.Add(GameEventType.StageCleared);
            return;
        }

        SpawnNext();
    }

    private void SpawnNext()
    {
        var type = NextPiece;
        NextPiece = _generator.Next();
        SoftDropping = false;

        var shape = Tetromino.Get(type);
        var spawn = new Piece(type, 0, (Board.Width - shape.Size) / 2, 0);
        Current = spawn;

        if (!Board.CanPlace(spawn))
        {
            Phase = GamePhase.GameOver;
            _events.Add(GameEventType.GameOver);
        }
    }
}
