# AIAU-tetris

Windows ネイティブ版テトリスの仕様・実装リポジトリ。仕様は [SPEC.md](SPEC.md) を参照。

## 構成

| パス | 内容 |
| --- | --- |
| `src/Tetris.Core` | UI 非依存のゲームロジック (盤面・テトロミノ・スコア・ステージ) |
| `src/Tetris.App` | WPF アプリ (描画・入力・音声・設定保存) |
| `tests/Tetris.Core.Tests` | Core の xUnit ユニットテスト |
| `tools/gen_stages.py` | 20 面のステージデータ (`src/Tetris.Core/Data/stages.csv`) 生成スクリプト |

## 必要環境

- Windows 10/11 x64
- .NET 8 SDK

## ビルド・テスト

```powershell
dotnet build
dotnet test
```

## 実行

```powershell
dotnet run --project src/Tetris.App
```

## 配布用ビルド (自己完結・単一ファイル)

```powershell
dotnet publish src/Tetris.App -p:PublishProfile=win-x64
```

出力: `src/Tetris.App/bin/publish/win-x64/Tetris.App.exe`

## 操作

| キー | 動作 |
| --- | --- |
| ← → | 左右移動 |
| ↑ | 右回転 |
| ↓ | ソフトドロップ (10 倍速) |
| Space | ハードドロップ (即固定) |
| P | ポーズ / 再開 |
