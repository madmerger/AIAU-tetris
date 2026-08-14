# AIAU-tetris

Windows ネイティブ版テトリスの仕様・実装リポジトリ。

仕様は [SPEC.md](SPEC.md) を参照。

## 構成

| プロジェクト | 内容 |
| --- | --- |
| `src/Tetris.Core` | UI 非依存のゲームロジック（盤面・ピース・スコア・ステージ） |
| `src/Tetris.App` | WPF アプリ（描画・入力・音声） |
| `tests/Tetris.Core.Tests` | Core のユニットテスト（xUnit） |
| `tools/generate_stages.py` | `src/Tetris.Core/Stages.csv`（20 面）の生成スクリプト |

## 必要環境

- Windows 10/11 x64
- .NET 8 SDK

## ビルド・テスト・実行

```powershell
dotnet build
dotnet test
dotnet run --project src/Tetris.App
```

## 配布用ビルド（自己完結・単一ファイル）

```powershell
dotnet publish src/Tetris.App/Tetris.App.csproj -c Release
```

出力: `src/Tetris.App/bin/Release/net8.0-windows/win-x64/publish/AIAUTetris.exe`

## 操作

| キー | 動作 |
| --- | --- |
| ← → | 左右移動 |
| ↑ | 時計回り回転 |
| ↓ | ソフトドロップ（10 倍速） |
| Space | ハードドロップ（即固定） |
| P | ポーズ / 再開 |

設定（ミュート状態）は `%APPDATA%\AIAUTetris\settings.json` に保存される。
