using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Tetris.App.Audio;
using Tetris.App.Rendering;
using Tetris.Core;

namespace Tetris.App;

public partial class MainWindow : Window
{
    private const double CellSize = 30;
    private static readonly TimeSpan MoveRepeatDelay = TimeSpan.FromSeconds(0.17);
    private static readonly TimeSpan MoveRepeatInterval = TimeSpan.FromSeconds(0.05);
    private static readonly TimeSpan MaxFrameDelta = TimeSpan.FromSeconds(0.1);
    private static readonly TimeSpan AllClearHold = TimeSpan.FromSeconds(4);

    private readonly UserSettings _settings = UserSettings.Load();
    private readonly AudioEngine _audio = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly BoardRenderer _boardRenderer;
    private readonly NextPieceRenderer _nextRenderer;

    private TetrisGame? _game;
    private GameMode _selectedMode = GameMode.Infinite;
    private GamePhase _lastPhase = GamePhase.Playing;
    private TimeSpan _lastTick;
    private TimeSpan _leftTimer;
    private TimeSpan _rightTimer;
    private TimeSpan _allClearTimer;
    private bool _leftHeld;
    private bool _rightHeld;

    public MainWindow()
    {
        InitializeComponent();

        _boardRenderer = new BoardRenderer(BoardCanvas, CellSize);
        _nextRenderer = new NextPieceRenderer(NextCanvas, 22);
        _audio.Muted = _settings.Muted;

        UpdateMuteButton();
        UpdateModeSelection();
        CompositionTarget.Rendering += OnRendering;
        Closed += (_, _) =>
        {
            CompositionTarget.Rendering -= OnRendering;
            _audio.Dispose();
        };
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var now = _clock.Elapsed;
        var delta = now - _lastTick;
        _lastTick = now;

        if (_game is null || GameView.Visibility != Visibility.Visible)
        {
            return;
        }

        if (delta > MaxFrameDelta)
        {
            delta = MaxFrameDelta;
        }

        ApplyHeldKeys(delta);
        _game.Update(delta);

        foreach (var sound in _game.DrainSounds())
        {
            _audio.Play(sound);
        }

        HandlePhaseChange(delta);
        UpdateVisuals();
    }

    private void ApplyHeldKeys(TimeSpan delta)
    {
        if (_game is null || _game.Phase != GamePhase.Playing)
        {
            return;
        }

        if (_leftHeld)
        {
            _leftTimer -= delta;
            if (_leftTimer <= TimeSpan.Zero)
            {
                _game.MoveLeft();
                _leftTimer = MoveRepeatInterval;
            }
        }

        if (_rightHeld)
        {
            _rightTimer -= delta;
            if (_rightTimer <= TimeSpan.Zero)
            {
                _game.MoveRight();
                _rightTimer = MoveRepeatInterval;
            }
        }
    }

    private void HandlePhaseChange(TimeSpan delta)
    {
        if (_game is null)
        {
            return;
        }

        if (_game.Phase != _lastPhase)
        {
            _lastPhase = _game.Phase;
            switch (_game.Phase)
            {
                case GamePhase.GameOver:
                case GamePhase.AllClear:
                    _audio.StopBgm();
                    _allClearTimer = _game.Phase == GamePhase.AllClear ? AllClearHold : TimeSpan.Zero;
                    break;
                case GamePhase.Paused:
                    _audio.BgmSuspended = true;
                    break;
                default:
                    _audio.BgmSuspended = false;
                    break;
            }
        }

        if (_game.Phase == GamePhase.AllClear && _allClearTimer > TimeSpan.Zero)
        {
            _allClearTimer -= delta;
            if (_allClearTimer <= TimeSpan.Zero)
            {
                ReturnToModeSelect();
            }
        }
    }

    private void UpdateVisuals()
    {
        if (_game is null)
        {
            return;
        }

        _boardRenderer.Render(_game);
        _nextRenderer.Render(_game.NextPiece);

        TimeText.Text = TimeDisplay.Format(_game.Elapsed);
        ScoreText.Text = _game.Score.ToString();
        LevelText.Text = _game.Level.ToString();
        LinesText.Text = _game.LinesCleared.ToString();
        StageText.Text = $"{_game.StageNumber} / {_game.StageCount}";
        PauseButton.Content = _game.Phase == GamePhase.Paused ? "再開 (P)" : "ポーズ (P)";

        var (title, detail) = _game.Phase switch
        {
            GamePhase.Paused => ("PAUSE", "P キーで再開"),
            GamePhase.GameOver => ("GAME OVER", $"SCORE {_game.Score}\nリスタートで再挑戦"),
            GamePhase.StageClear => ("GAME CLEAR", $"次のステージまで {_game.CountdownSeconds}"),
            GamePhase.AllClear => ("ALL CLEAR", "全ステージクリア！\nモード選択へ戻ります"),
            _ => (string.Empty, string.Empty),
        };

        OverlayTitle.Text = title;
        OverlayDetail.Text = detail;
        OverlayPanel.Visibility = title.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    private void StartGame()
    {
        try
        {
            _game = new TetrisGame(_selectedMode);
        }
        catch (StageDataException ex)
        {
            MessageBox.Show(this, ex.Message, "ステージデータ エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        _lastPhase = _game.Phase;
        _leftHeld = false;
        _rightHeld = false;
        _allClearTimer = TimeSpan.Zero;

        ModeText.Text = _selectedMode == GameMode.Stage ? "STAGE" : "INFINITE";
        var stageVisibility = _selectedMode == GameMode.Stage ? Visibility.Visible : Visibility.Collapsed;
        StageLabel.Visibility = stageVisibility;
        StageText.Visibility = stageVisibility;

        ModeSelectView.Visibility = Visibility.Collapsed;
        GameView.Visibility = Visibility.Visible;

        _audio.BgmSuspended = false;
        _audio.StartBgm();
        foreach (var sound in _game.DrainSounds())
        {
            _audio.Play(sound);
        }

        UpdateVisuals();
    }

    private void ReturnToModeSelect()
    {
        _audio.StopBgm();
        _game = null;
        GameView.Visibility = Visibility.Collapsed;
        ModeSelectView.Visibility = Visibility.Visible;
        OverlayPanel.Visibility = Visibility.Collapsed;
    }

    private void UpdateModeSelection()
    {
        StageModeButton.IsChecked = _selectedMode == GameMode.Stage;
        InfiniteModeButton.IsChecked = _selectedMode == GameMode.Infinite;
        ModeDescription.Text = _selectedMode == GameMode.Stage
            ? "20 面の固定配置。ライン消去でジェムを全て消すとステージクリア。"
            : "空の盤面でゲームオーバーまで続く通常プレイ。";
    }

    private void UpdateMuteButton() => MuteButton.Content = $"ミュート: {(_settings.Muted ? "ON" : "OFF")}";

    private void ModeButton_Click(object sender, RoutedEventArgs e)
    {
        _selectedMode = ReferenceEquals(sender, StageModeButton) ? GameMode.Stage : GameMode.Infinite;
        UpdateModeSelection();
        _audio.Play(SoundEffect.MenuMove);
    }

    private void StartButton_Click(object sender, RoutedEventArgs e) => StartGame();

    private void PauseButton_Click(object sender, RoutedEventArgs e) => _game?.TogglePause();

    private void RestartButton_Click(object sender, RoutedEventArgs e)
    {
        if (_game is null)
        {
            return;
        }

        _game.Restart();
        _lastPhase = _game.Phase;
        _audio.BgmSuspended = false;
        _audio.StartBgm();
        _game.DrainSounds();
        _audio.Play(SoundEffect.GameStart);
    }

    private void MuteButton_Click(object sender, RoutedEventArgs e)
    {
        _settings.Muted = !_settings.Muted;
        _audio.Muted = _settings.Muted;
        _settings.Save();
        UpdateMuteButton();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        _audio.Play(SoundEffect.MenuMove);
        ReturnToModeSelect();
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (_game is null || GameView.Visibility != Visibility.Visible)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Left:
                if (!_leftHeld)
                {
                    _leftHeld = true;
                    _leftTimer = MoveRepeatDelay;
                    _game.MoveLeft();
                }

                e.Handled = true;
                break;
            case Key.Right:
                if (!_rightHeld)
                {
                    _rightHeld = true;
                    _rightTimer = MoveRepeatDelay;
                    _game.MoveRight();
                }

                e.Handled = true;
                break;
            case Key.Up:
                _game.RotateClockwise();
                e.Handled = true;
                break;
            case Key.Down:
                _game.SoftDropping = _game.Phase == GamePhase.Playing;
                e.Handled = true;
                break;
            case Key.Space:
                if (!e.IsRepeat)
                {
                    _game.HardDrop();
                }

                e.Handled = true;
                break;
            case Key.P:
                if (!e.IsRepeat)
                {
                    _game.TogglePause();
                }

                e.Handled = true;
                break;
        }
    }

    protected override void OnPreviewKeyUp(KeyEventArgs e)
    {
        base.OnPreviewKeyUp(e);

        switch (e.Key)
        {
            case Key.Left:
                _leftHeld = false;
                break;
            case Key.Right:
                _rightHeld = false;
                break;
            case Key.Down:
                if (_game is not null)
                {
                    _game.SoftDropping = false;
                }

                break;
        }
    }
}
