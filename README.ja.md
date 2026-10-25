# yhwndy

[English](README.md)

ホットキーで、ほかのアプリのウィンドウを半透明にしたり、タイトルバーと枠を消したりするWindowsの常駐ツール。Ghost Modeにしたウィンドウは、マウスカーソルが近づくと薄くなる。

## 特徴

- 透明度: 前面のウィンドウの不透明度を、5%から100%の間で5%ずつ変更する
- ボーダーレス: 前面のウィンドウのタイトルバーと枠を消す。最大化したウィンドウは、タスクバーに重ねてモニター全体に広げる
- Ghost Mode: マウスカーソルが近づくほど、ウィンドウを薄くする。カーソルがウィンドウの上か、ウィンドウの端から50px未満にあると、不透明度は約5%に下がる。端から50pxと150pxの間では、カーソルが離れるほど、`Ctrl+Alt+↑/↓`で設定した不透明度に近づく。Ctrlを押している間は、設定した不透明度で表示する
- Ghost Modeのウィンドウは、Ctrlを押しながらドラッグすると移動できる。端から16px以内でドラッグを始めると、移動ではなくサイズを変更する。最大化したウィンドウはドラッグできない

変更はウィンドウごとに適用し、保存しない。yhwndyを終了すると、変更したウィンドウをすべて元に戻す。

## インストール

x64のWindowsで動作する。[Scoop](https://scoop.sh/)でインストールする。

```powershell
scoop bucket add yscoopy https://github.com/ycookiey/yscoopy
scoop install yscoopy/yhwndy
```

または、[Releases](https://github.com/ycookiey/yhwndy/releases)から`yhwndy.exe`をダウンロードして起動する。`yhwndy.exe`は.NETのランタイムを含むため、.NETのインストールは不要。

## 使い方

`yhwndy.exe`を起動すると、タスクトレイにWindows標準のアプリのアイコンが表示される。ホットキーで変更するのは前面のウィンドウ。

| ホットキー | 動作 |
|---|---|
| `Ctrl+Alt+↑` | 不透明度を5%上げる |
| `Ctrl+Alt+↓` | 不透明度を5%下げる |
| `Ctrl+Alt+B` | ボーダーレスのオンとオフを切り替える |
| `Ctrl+Alt+G` | Ghost Modeのオンとオフを切り替える |

ホットキーは変更できない。ほかのアプリが同じキーを登録していると、yhwndyは起動時に「一部ホットキーが登録できませんでした」と通知する。登録できなかったキーは効かない。

タスクトレイのアイコンを右クリックすると、次のメニューが開く。

- 変更中のウィンドウ: yhwndyが変更中のウィンドウの数
- スタートアップ登録: チェックを付けると、サインイン時にyhwndyが自動で起動する
- 緊急リセット: 変更したウィンドウをすべて元に戻す
- 終了: 変更したウィンドウをすべて元に戻して、yhwndyを終了する

## 既知の制限

- Ghost Modeのウィンドウは、薄くなってもクリックを受け付ける
- 最大化していないGhost Modeのウィンドウでは、yhwndyがCtrl+クリックを移動とサイズの変更に使うため、Ctrlを押しながらクリックしてもアプリにはクリックが届かない
- 管理者として実行しているアプリのウィンドウを変更するには、yhwndyも管理者として実行する必要がある
- タスクマネージャーなどでyhwndyを強制終了すると、変更したウィンドウは閉じるまで変更されたまま残る。yhwndyを再起動しても元に戻らない

## アンインストール

1. タスクトレイのメニューの「スタートアップ登録」のチェックを外す。外さないと、レジストリのキー`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`に`yhwndy`の値が残る
2. タスクトレイのメニューの「終了」でyhwndyを終了する
3. Scoopでインストールした場合は`scoop uninstall yhwndy`を実行する。Releasesからダウンロードした場合は`yhwndy.exe`を削除する

## ビルド

.NET 8のSDKでビルドする。

```powershell
cd src/yhwndy
dotnet publish -c Release
```

出力先は`src/yhwndy/bin/Release/net8.0-windows/win-x64/publish/yhwndy.exe`。

## ライセンス

[MIT License](LICENSE)
