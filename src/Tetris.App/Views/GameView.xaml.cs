using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Tetris.App.Audio;
using Tetris.Core;

namespace Tetris.App.Views;

/// <summary>ゲーム画面。60FPS 想定のフレームループでロジックを進め、盤面を毎フレーム描画する。</summary>
public partial class GameView : UserControl
{
    private const double MoveRepeatDelaySeconds = 0.17;
    private const double MoveRepeatIntervalSeconds = 0.045;
    private const double MaxFrameSeconds = 0.1;

    private readonly Stopwatch _clock = new();
    private AudioEngine? _audio;
    private TetrisGame? _game;
    private bool _loopAttached;
    private long _lastTicks;
    private int _horizontalDirection;
    private double _repeatTimer;

    public GameView()
    {
        InitializeComponent();
    }

    /// <summary>モード選択画面へ戻る要求。</summary>
    public event EventHandler? BackRequested;

    /// <summary>ミュート設定が変更された（永続化用）。</summary>
    public event EventHandler<bool>? MuteChanged;

    public TetrisGame? Game => _game;

    public void Initialize(AudioEngine audio)
    {
        _audio = audio;
        UpdateMuteButton();
    }

    public void StartGame(GameMode mode)
    {
        _game = new TetrisGame(mode, new BagPieceGenerator());
        Board.Game = _game;
        _horizontalDirection = 0;
        _repeatTimer = 0;

        ModeText.Text = mode == GameMode.Stage ? "STAGE MODE" : "INFINITE MODE";
        var stageVisibility = mode == GameMode.Stage ? Visibility.Visible : Visibility.Collapsed;
        StageLabel.Visibility = stageVisibility;
        StageText.Visibility = stageVisibility;
        GemLabel.Visibility = stageVisibility;
        GemText.Visibility = stageVisibility;

        if (_audio is not null)
        {
            _audio.PauseMuted = false;
            _audio.Play(SoundEffect.GameStart);
            _audio.StopBgm();
            _audio.StartBgm();
        }

        AttachLoop();
        UpdateHud();
    }

    public void StopGame()
    {
        DetachLoop();
        _audio?.StopBgm();
        _game = null;
        Board.Game = null;
    }

    /// <summary>ウィンドウから転送されたキー押下を処理する。</summary>
    public bool HandleKeyDown(Key key, bool isRepeat)
    {
        if (_game is null)
        {
            return false;
        }

        switch (key)
        {
            case Key.Left:
                if (!isRepeat)
                {
                    StartHorizontal(-1);
                }

                return true;
            case Key.Right:
                if (!isRepeat)
                {
                    StartHorizontal(1);
                }

                return true;
            case Key.Down:
                _game.SoftDropping = true;
                return true;
            case Key.Up:
                if (!isRepeat)
                {
                    _game.RotateClockwise();
                }

                return true;
            case Key.Space:
                if (!isRepeat)
                {
                    _game.HardDrop();
                }

                return true;
            case Key.P:
                if (!isRepeat)
                {
                    TogglePause();
                }

                return true;
            default:
                return false;
        }
    }

    /// <summary>ウィンドウから転送されたキー解放を処理する。</summary>
    public bool HandleKeyUp(Key key)
    {
        if (_game is null)
        {
            return false;
        }

        switch (key)
        {
            case Key.Left:
                if (_horizontalDirection < 0)
                {
                    _horizontalDirection = 0;
                }

                return true;
            case Key.Right:
                if (_horizontalDirection > 0)
                {
                    _horizontalDirection = 0;
                }

                return true;
            case Key.Down:
                _game.SoftDropping = false;
                return true;
            default:
                return false;
        }
    }

    private void StartHorizontal(int direction)
    {
        if (_game is null)
        {
            return;
        }

        _horizontalDirection = direction;
        _repeatTimer = MoveRepeatDelaySeconds;
        Move(direction);
    }

    private void Move(int direction)
    {
        if (direction < 0)
        {
            _game?.MoveLeft();
        }
        else if (direction > 0)
        {
            _game?.MoveRight();
        }
    }

    private void TogglePause()
    {
        if (_game is null)
        {
            return;
        }

        _game.TogglePause();
        var paused = _game.Phase == GamePhase.Paused;
        PauseButton.Content = paused ? "再開 (P)" : "ポーズ (P)";
        if (_audio is not null)
        {
            _audio.PauseMuted = paused;
        }
    }

    private void AttachLoop()
    {
        if (_loopAttached)
        {
            return;
        }

        _clock.Restart();
        _lastTicks = _clock.ElapsedTicks;
        CompositionTarget.Rendering += OnRendering;
        _loopAttached = true;
    }

    private void DetachLoop()
    {
        if (!_loopAttached)
        {
            return;
        }

        CompositionTarget.Rendering -= OnRendering;
        _loopAttached = false;
        _clock.Stop();
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (_game is null)
        {
            return;
        }

        var ticks = _clock.ElapsedTicks;
        var delta = Math.Min((double)(ticks - _lastTicks) / Stopwatch.Frequency, MaxFrameSeconds);
        _lastTicks = ticks;

        UpdateHorizontalRepeat(delta);
        _game.Update(delta);
        ProcessGameEvents();

        Board.InvalidateVisual();
        UpdateHud();
    }

    private void UpdateHorizontalRepeat(double delta)
    {
        if (_horizontalDirection == 0 || _game is null || _game.Phase != GamePhase.Playing)
        {
            return;
        }

        _repeatTimer -= delta;
        while (_repeatTimer <= 0)
        {
            Move(_horizontalDirection);
            _repeatTimer += MoveRepeatIntervalSeconds;
        }
    }

    private void ProcessGameEvents()
    {
        if (_game is null)
        {
            return;
        }

        foreach (var gameEvent in _game.DrainEvents())
        {
            switch (gameEvent)
            {
                case GameEventType.PieceLocked:
                    _audio?.Play(SoundEffect.PieceLock);
                    break;
                case GameEventType.StageCleared:
                    _audio?.Play(SoundEffect.StageClear);
                    break;
                case GameEventType.AllCleared:
                    _audio?.Play(SoundEffect.StageClear);
                    _audio?.StopBgm();
                    break;
                case GameEventType.GameOver:
                    _audio?.StopBgm();
                    break;
            }
        }
    }

    private void UpdateHud()
    {
        if (_game is null)
        {
            return;
        }

        var elapsed = TimeSpan.FromSeconds(_game.ElapsedSeconds);
        TimeText.Text = $"{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}:{elapsed.Milliseconds / 10:00}";
        ScoreText.Text = _game.Score.ToString("N0");
        LevelText.Text = _game.Level.ToString();
        LinesText.Text = _game.TotalLines.ToString();
        Next.Piece = _game.NextPiece;
        Next.InvalidateVisual();

        if (_game.Mode == GameMode.Stage)
        {
            StageText.Text = $"{_game.StageNumber} / {_game.StageCount}";
            GemText.Text = _game.RemainingGems.ToString();
        }

        PauseButton.IsEnabled = _game.Phase is GamePhase.Playing or GamePhase.Paused;
    }

    private void OnPauseClicked(object sender, RoutedEventArgs e) => TogglePause();

    private void OnRestartClicked(object sender, RoutedEventArgs e)
    {
        if (_game is null)
        {
            return;
        }

        _game.Restart();
        PauseButton.Content = "ポーズ (P)";
        _horizontalDirection = 0;
        if (_audio is not null)
        {
            _audio.PauseMuted = false;
            _audio.Play(SoundEffect.GameStart);
            _audio.StopBgm();
            _audio.StartBgm();
        }

        UpdateHud();
    }

    private void OnMuteClicked(object sender, RoutedEventArgs e)
    {
        if (_audio is null)
        {
            return;
        }

        _audio.Muted = !_audio.Muted;
        UpdateMuteButton();
        MuteChanged?.Invoke(this, _audio.Muted);
        _audio.Play(SoundEffect.MenuMove);
    }

    private void OnBackClicked(object sender, RoutedEventArgs e)
    {
        _audio?.Play(SoundEffect.MenuMove);
        StopGame();
        BackRequested?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateMuteButton()
        => MuteButton.Content = _audio?.Muted == true ? "ミュート: ON" : "ミュート: OFF";
}
