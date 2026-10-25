using Microsoft.Win32;

namespace yhwndy;

/// <summary>
/// Hidden form that owns the tray icon, the hotkeys and the mouse hook.
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
    
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    
    private Icon _trayIconImage;
    private bool _trayIconIsLight;
    
    public MainForm()
    {
        // The form is never shown; it only receives the hotkey messages.
        ShowInTaskbar = false;
        WindowState = FormWindowState.Minimized;
        FormBorderStyle = FormBorderStyle.None;
        Opacity = 0;
        
        _windowManager = new WindowManager(Handle);
        _hotkeyManager = new HotkeyManager(Handle);
        _mouseHook = new MouseHook(_windowManager);
        _ghostModeManager = new GhostModeManager(_windowManager, _mouseHook);
        
        _contextMenu = CreateContextMenu();
        
        _trayIconIsLight = IsTaskbarLight();
        _trayIconImage = LoadTrayIcon(_trayIconIsLight);
        
        _trayIcon = new NotifyIcon
        {
            Icon = _trayIconImage,
            Text = "yhwndy - ウィンドウ制御ツール",
            Visible = true,
            ContextMenuStrip = _contextMenu
        };
        
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        
        _hotkeyManager.RegisterAll();
        if (_hotkeyManager.HasFailedRegistration)
        {
            ShowNotification("一部ホットキーが登録できませんでした");
        }
    }
    
    private ContextMenuStrip CreateContextMenu()
    {
        var menu = new ContextMenuStrip();
        
        _statusMenuItem = new ToolStripMenuItem("変更中のウィンドウ: なし") { Enabled = false };
        menu.Items.Add(_statusMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        
        _startupMenuItem = new ToolStripMenuItem("スタートアップ登録")
        {
            Checked = StartupManager.IsRegistered,
            CheckOnClick = true
        };
        _startupMenuItem.Click += (_, _) => StartupManager.Toggle();
        menu.Items.Add(_startupMenuItem);
        
        menu.Items.Add(new ToolStripSeparator());
        
        var resetItem = new ToolStripMenuItem("緊急リセット");
        resetItem.Click += (_, _) =>
        {
            _windowManager.ResetAll();
            _ghostModeManager.Refresh();
            ShowNotification("全ウィンドウをリセットしました");
        };
        menu.Items.Add(resetItem);
        
        menu.Items.Add(new ToolStripSeparator());
        
        var exitItem = new ToolStripMenuItem("終了");
        exitItem.Click += (_, _) => Application.Exit();
        menu.Items.Add(exitItem);
        
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
        
        // The Run value can be changed outside yhwndy, so read it again.
        if (_startupMenuItem != null)
        {
            _startupMenuItem.Checked = StartupManager.IsRegistered;
        }
    }
    
    private void ShowNotification(string message)
    {
        _trayIcon.ShowBalloonTip(3000, "yhwndy", message, ToolTipIcon.Info);
    }
    
    /// <summary>
    /// Whether the taskbar uses the light theme. A missing or unreadable value means dark.
    /// </summary>
    private static bool IsTaskbarLight()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey, false);
            return key?.GetValue("SystemUsesLightTheme") is int value && value == 1;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Loads the tray icon for the taskbar theme (black lines on light, white lines on dark) at the small icon size.
    /// </summary>
    private static Icon LoadTrayIcon(bool light)
    {
        string name = light ? "tray-light.ico" : "tray-dark.ico";
        using var stream = typeof(MainForm).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded resource {name} not found");
        return new Icon(stream, SystemInformation.SmallIconSize);
    }
    
    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        // A theme change arrives as WM_SETTINGCHANGE "ImmersiveColorSet", which SystemEvents reports as General.
        if (e.Category != UserPreferenceCategory.General) return;
        
        bool light = IsTaskbarLight();
        if (light == _trayIconIsLight) return;
        
        var oldIcon = _trayIconImage;
        _trayIconImage = LoadTrayIcon(light);
        _trayIconIsLight = light;
        _trayIcon.Icon = _trayIconImage;
        oldIcon.Dispose();
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
                    _windowManager.ChangeOpacity(hwnd, 5);
                    break;
                    
                case HotkeyManager.HOTKEY_OPACITY_DOWN:
                    _windowManager.ChangeOpacity(hwnd, -5);
                    break;
                    
                case HotkeyManager.HOTKEY_BORDERLESS:
                    _windowManager.ToggleBorderless(hwnd);
                    break;
                    
                case HotkeyManager.HOTKEY_GHOST_MODE:
                    _windowManager.ToggleGhostMode(hwnd);
                    _ghostModeManager.Refresh();
                    break;
            }
        }
        
        base.WndProc(ref m);
    }
    
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _windowManager.ResetAll();
        
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _mouseHook.Dispose();
        _ghostModeManager.Dispose();
        _hotkeyManager.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _trayIconImage.Dispose();
        
        base.OnFormClosing(e);
    }
    
    protected override void SetVisibleCore(bool value)
    {
        // Create the handle for the hotkeys without ever showing the form.
        if (!IsHandleCreated)
        {
            CreateHandle();
            value = false;
        }
        base.SetVisibleCore(value);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            // Keep the hidden form out of the Alt+Tab list.
            cp.ExStyle |= (int)NativeMethods.WS_EX_TOOLWINDOW;
            return cp;
        }
    }
}
