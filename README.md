# yhwndy

[日本語](README.ja.md)

A Windows tray tool that changes the windows of other apps with hotkeys: make a window semi-transparent or remove its title bar and border. In Ghost Mode, a window fades out as the mouse cursor comes near it.

## Features

- Transparency: change the opacity of the active window in 5% steps, from 5% to 100%
- Borderless: hide the title bar and border of the active window. A maximized window is stretched over the whole monitor, covering the taskbar
- Ghost Mode: the window fades as the mouse cursor comes near it. Over the window or within 50 px of its edge, the opacity is about 5%. From 50 px to 150 px away, it rises back to the opacity set with `Ctrl+Alt+↑/↓`. Hold Ctrl to show the window at that set opacity
- Ctrl+drag a window in Ghost Mode to move it, or start the drag within 16 px of an edge to resize it. Maximized windows can't be dragged

Changes are applied per window and aren't saved. Quitting yhwndy restores every changed window.

## Install

Runs on 64-bit (x64) Windows. Install with [Scoop](https://scoop.sh/):

```powershell
scoop bucket add yscoopy https://github.com/ycookiey/yscoopy
scoop install yscoopy/yhwndy
```

Or download `yhwndy.exe` from [Releases](https://github.com/ycookiey/yhwndy/releases) and run it. The .NET runtime is bundled in the exe, so you don't need to install .NET.

## Usage

Start `yhwndy.exe`. The generic Windows application icon appears in the tray. The hotkeys act on the active window:

| Hotkey | Action |
|---|---|
| `Ctrl+Alt+↑` | Raise the opacity by 5% |
| `Ctrl+Alt+↓` | Lower the opacity by 5% |
| `Ctrl+Alt+B` | Turn borderless on or off |
| `Ctrl+Alt+G` | Turn Ghost Mode on or off |

The hotkeys can't be changed. If another app has already registered one of them, yhwndy shows the notification `一部ホットキーが登録できませんでした` at startup, and that hotkey doesn't work.

Right-click the tray icon to open the menu. The menu is in Japanese:

- `変更中のウィンドウ`: the number of windows yhwndy has changed
- `スタートアップ登録`: when checked, yhwndy starts when you sign in
- `緊急リセット`: restore every changed window
- `終了`: restore every changed window and quit

## Known limitations

- A window in Ghost Mode still receives clicks while it is faded
- In a window in Ghost Mode that isn't maximized, Ctrl+click is used for moving and resizing and doesn't reach the app
- To change the windows of an app running as administrator, yhwndy must also run as administrator
- If yhwndy is ended forcibly, for example from Task Manager, the changed windows stay changed until they are closed. Restarting yhwndy doesn't restore them

## Uninstall

1. Uncheck `スタートアップ登録` in the tray menu. Otherwise the `yhwndy` value is left in the registry key `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
2. Quit with `終了` in the tray menu
3. If you installed with Scoop, run `scoop uninstall yhwndy`. If you downloaded it from Releases, delete `yhwndy.exe`

## Build

Requires the .NET 8 SDK.

```powershell
cd src/yhwndy
dotnet publish -c Release
```

The output is `src/yhwndy/bin/Release/net8.0-windows/win-x64/publish/yhwndy.exe`.

## License

[MIT License](LICENSE)
