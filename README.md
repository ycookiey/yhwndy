# yhwndy

Windows向けウィンドウ制御ツール。透明化・ボーダーレス・Ghost Modeで視認性と操作性を向上。

## 主な機能

### 1. 透明化 (Transparency)

- ホットキーで透明度を調整（5%刻み）
- 透明度はウィンドウごとに管理

### 2. ボーダーレス化 (Borderless)

- タイトルバーと枠線を非表示
- 最大化時は画面全体に拡張（タスクバーが隠れる場合あり）
- タスクバー/Alt+Tabの表示は維持

### 3. Ghost Mode（距離に応じた透明度の自動制御）

- マウスとの距離で透明度を自動調整（ウィンドウごとにON/OFF）
- Ctrl押下中は設定透明度で固定
- 操作: `Ctrl+ドラッグ`（中央付近: 移動 / 端（16px以内）: リサイズ）

## システム要件

- Windows 10/11
- .NET 8 Runtime（自己完結型ビルドの場合は不要）

## ホットキー

| 機能 | ホットキー |
|------|-----------|
| 透明度を上げる | `Ctrl+Alt+↑` |
| 透明度を下げる | `Ctrl+Alt+↓` |
| ボーダーレス切替 | `Ctrl+Alt+B` |
| Ghost Mode切替 | `Ctrl+Alt+G` |
| 移動/リサイズ | `Ctrl+ドラッグ` |

## タスクトレイメニュー

- **状態表示**: 設定適用中のウィンドウ一覧を表示
- **スタートアップ登録**: Windows起動時に自動起動
- **緊急リセット**: すべてのウィンドウ設定をリセット
- **終了**: 終了して適用したスタイルを復元

## インストール

1. [Releases](https://github.com/ycookiey/yhwndy/releases)から最新版をダウンロード
2. `yhwndy.exe`を任意のフォルダーに配置
3. 実行して使用開始

**注意**: 管理者権限アプリ操作時は、本ツールも管理者で実行。

## ビルド方法

```bash
cd src/yhwndy
dotnet build
```

### リリースビルド

```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```
