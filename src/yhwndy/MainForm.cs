namespace yhwndy;

/// <summary>
/// タスクトレイ常駐フォーム
/// </summary>
public class MainForm : Form
{
    private readonly NotifyIcon _trayIcon;
    private readonly ContextMenuStrip _contextMenu;
    private readonly WindowManager _windowManager;
    private readonly HotkeyManager _hotkeyManager;
    private readonly GhostModeManager _ghostModeManager;
    private readonly MouseHook _mouseHook;
    
    private ToolStripMenuItem? _startupMenuItem;
    private ToolStripMenuItem? _statusMenuItem;
    
    public MainForm()
    {
        // フォームを非表示に
        ShowInTaskbar = false;
        WindowState = FormWindowState.Minimized;
        FormBorderStyle = FormBorderStyle.None;
        Opacity = 0;
        
        // マネージャー初期化
        _windowManager = new WindowManager(Handle);
        _hotkeyManager = new HotkeyManager(Handle);
        _ghostModeManager = new GhostModeManager(_windowManager);
        _mouseHook = new MouseHook(_windowManager);
        
        // コンテキストメニュー作成
        _contextMenu = CreateContextMenu();
        
        // タスクトレイアイコン
        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application, // 仮アイコン
            Text = "yhwndy - ウィンドウ制御ツール",
            Visible = true,
            ContextMenuStrip = _contextMenu
        };
        
        // ホットキー登録
        _hotkeyManager.RegisterAll();
        if (_hotkeyManager.HasFailedRegistration)
        {
            ShowNotification("一部ホットキーが登録できませんでした");
        }
        
        // マウスフック開始
        _mouseHook.Start();
    }
    
    private ContextMenuStrip CreateContextMenu()
    {
        var menu = new ContextMenuStrip();
        
        // 状態表示（動的更新）
        _statusMenuItem = new ToolStripMenuItem("変更中のウィンドウ: なし") { Enabled = false };
        menu.Items.Add(_statusMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        
        // スタートアップ登録
        _startupMenuItem = new ToolStripMenuItem("スタートアップ登録")
        {
            Checked = StartupManager.IsRegistered,
            CheckOnClick = true
        };
        _startupMenuItem.Click += (_, _) => StartupManager.Toggle();
        menu.Items.Add(_startupMenuItem);
        
        menu.Items.Add(new ToolStripSeparator());
        
        // 緊急リセット
        var resetItem = new ToolStripMenuItem("緊急リセット");
        resetItem.Click += (_, _) =>
        {
            _windowManager.ResetAll();
            ShowNotification("全ウィンドウをリセットしました");
        };
        menu.Items.Add(resetItem);
        
        menu.Items.Add(new ToolStripSeparator());
        
        // 終了
        var exitItem = new ToolStripMenuItem("終了");
        exitItem.Click += (_, _) => Application.Exit();
        menu.Items.Add(exitItem);
        
        // メニュー表示時に状態を更新
        menu.Opening += (_, _) => UpdateStatusMenu();
        
        return menu;
    }
    
    private void UpdateStatusMenu()
    {
        if (_statusMenuItem == null) return;
        
        var modified = _windowManager.ModifiedWindows.ToList();
        if (modified.Count == 0)
        {
            _statusMenuItem.Text = "変更中のウィンドウ: なし";
        }
        else
        {
            _statusMenuItem.Text = $"変更中のウィンドウ: {modified.Count}";
        }
        
        // スタートアップ状態も更新
        if (_startupMenuItem != null)
        {
            _startupMenuItem.Checked = StartupManager.IsRegistered;
        }
    }
    
    private void ShowNotification(string message)
    {
        _trayIcon.ShowBalloonTip(3000, "yhwndy", message, ToolTipIcon.Info);
    }
    
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY)
        {
            int id = m.WParam.ToInt32();
            IntPtr hwnd = NativeMethods.GetForegroundWindow();
            hwnd = _windowManager.GetTopLevelWindow(hwnd);
            
            switch (id)
            {
                case HotkeyManager.HOTKEY_OPACITY_UP:
                    _windowManager.ChangeOpacity(hwnd, 5); // 5%不透明に
                    break;
                    
                case HotkeyManager.HOTKEY_OPACITY_DOWN:
                    _windowManager.ChangeOpacity(hwnd, -5); // 5%透明に
                    break;
                    
                case HotkeyManager.HOTKEY_BORDERLESS:
                    _windowManager.ToggleBorderless(hwnd);
                    break;
                    
                case HotkeyManager.HOTKEY_GHOST_MODE:
                    _windowManager.ToggleGhostMode(hwnd);
                    break;
            }
        }
        
        base.WndProc(ref m);
    }
    
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // 全スタイル復元
        _windowManager.ResetAll();
        
        // リソース解放
        _mouseHook.Dispose();
        _ghostModeManager.Dispose();
        _hotkeyManager.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        
        base.OnFormClosing(e);
    }
    
    protected override void SetVisibleCore(bool value)
    {
        // 起動時に非表示
        if (!IsHandleCreated)
        {
            CreateHandle();
            value = false;
        }
        base.SetVisibleCore(value);
    }
}
