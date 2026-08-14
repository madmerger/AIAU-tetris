using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Tetris.Core;

namespace Tetris.App.Views;

/// <summary>モード選択画面。</summary>
public partial class ModeSelectView : UserControl
{
    private static readonly Brush SelectedBorder = new SolidColorBrush(Color.FromRgb(0x4C, 0xC9, 0xF0));
    private static readonly Brush SelectedBackground = new SolidColorBrush(Color.FromRgb(0x25, 0x3A, 0x4E));
    private static readonly Brush UnselectedBorder = new SolidColorBrush(Color.FromRgb(0x3C, 0x44, 0x62));
    private static readonly Brush UnselectedBackground = new SolidColorBrush(Color.FromRgb(0x1D, 0x21, 0x30));

    private GameMode _selectedMode = GameMode.Stage;

    public ModeSelectView()
    {
        InitializeComponent();
        UpdateSelectionVisuals();
    }

    /// <summary>選択されたモードでゲーム開始を要求する。</summary>
    public event EventHandler<GameMode>? StartRequested;

    /// <summary>メニュー操作音の再生を要求する。</summary>
    public event EventHandler? SelectionChanged;

    public GameMode SelectedMode
    {
        get => _selectedMode;
        private set
        {
            if (_selectedMode == value)
            {
                return;
            }

            _selectedMode = value;
            UpdateSelectionVisuals();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>ウィンドウから転送されたキー入力を処理する。</summary>
    public bool HandleKeyDown(Key key)
    {
        switch (key)
        {
            case Key.Left:
            case Key.Up:
                SelectedMode = GameMode.Stage;
                return true;
            case Key.Right:
            case Key.Down:
                SelectedMode = GameMode.Infinite;
                return true;
            case Key.Enter:
            case Key.Space:
                StartRequested?.Invoke(this, SelectedMode);
                return true;
            default:
                return false;
        }
    }

    private void OnStageOptionClicked(object sender, MouseButtonEventArgs e) => SelectedMode = GameMode.Stage;

    private void OnInfiniteOptionClicked(object sender, MouseButtonEventArgs e) => SelectedMode = GameMode.Infinite;

    private void OnStartClicked(object sender, RoutedEventArgs e) => StartRequested?.Invoke(this, SelectedMode);

    private void UpdateSelectionVisuals()
    {
        var stageSelected = _selectedMode == GameMode.Stage;
        StageOption.BorderBrush = stageSelected ? SelectedBorder : UnselectedBorder;
        StageOption.Background = stageSelected ? SelectedBackground : UnselectedBackground;
        InfiniteOption.BorderBrush = stageSelected ? UnselectedBorder : SelectedBorder;
        InfiniteOption.Background = stageSelected ? UnselectedBackground : SelectedBackground;
    }
}
