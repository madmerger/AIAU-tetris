using System;
using System.Windows;
using System.Windows.Input;
using Tetris.App.Audio;
using Tetris.Core;

namespace Tetris.App;

public partial class MainWindow : Window
{
    private readonly AudioEngine _audio = new();
    private readonly UserSettings _settings = UserSettings.Load();

    public MainWindow()
    {
        InitializeComponent();

        _audio.Muted = _settings.Muted;
        Game.Initialize(_audio);
        Game.BackRequested += OnBackToModeSelect;
        Game.MuteChanged += OnMuteChanged;
        ModeSelect.StartRequested += OnStartRequested;
        ModeSelect.SelectionChanged += (_, _) => _audio.Play(SoundEffect.MenuMove);
        Deactivated += (_, _) => Game.ResetInputState();
        Closed += (_, _) =>
        {
            Game.StopGame();
            _audio.Dispose();
        };
    }

    private void OnStartRequested(object? sender, GameMode mode)
    {
        ModeSelect.Visibility = Visibility.Collapsed;
        Game.Visibility = Visibility.Visible;
        Game.StartGame(mode);
    }

    private void OnBackToModeSelect(object? sender, EventArgs e)
    {
        Game.Visibility = Visibility.Collapsed;
        ModeSelect.Visibility = Visibility.Visible;
    }

    private void OnMuteChanged(object? sender, bool muted)
    {
        _settings.Muted = muted;
        _settings.Save();
    }

    private void OnWindowKeyDown(object sender, KeyEventArgs e)
    {
        var handled = Game.Visibility == Visibility.Visible
            ? Game.HandleKeyDown(e.Key, e.IsRepeat)
            : ModeSelect.HandleKeyDown(e.Key);
        e.Handled = handled;
    }

    private void OnWindowKeyUp(object sender, KeyEventArgs e)
    {
        if (Game.Visibility == Visibility.Visible)
        {
            e.Handled = Game.HandleKeyUp(e.Key);
        }
    }
}
