using System.Windows;
using Tetris.Core;

namespace Tetris.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // ステージデータの不整合は起動時に検出してエラーとして扱う（SPEC 4.）。
        try
        {
            _ = StageSet.Embedded;
        }
        catch (StageDataException ex)
        {
            MessageBox.Show(
                $"ステージデータを読み込めませんでした。\n\n{ex.Message}",
                "AIAU Tetris",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        base.OnStartup(e);
    }
}
