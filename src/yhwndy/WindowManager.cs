using System.Runtime.InteropServices;

namespace yhwndy;

/// <summary>
/// 個々のウィンドウの状態を管理する情報クラス
/// </summary>
public class WindowInfo
{
    public IntPtr Handle { get; }
    
    /// <summary>透明度 (5-100%, 内部では0-255)</summary>
    public byte OpacityPercent { get; set; } = 100;
    
    /// <summary>ボーダーレス状態か</summary>
    public bool IsBorderless { get; set; }
    
    /// <summary>Ghost Mode が有効か</summary>
    public bool IsGhostMode { get; set; }
    
    /// <summary>元のウィンドウスタイル（復元用）</summary>
    public int OriginalStyle { get; set; }
    
    /// <summary>元のウィンドウ拡張スタイル（復元用）</summary>
    public int OriginalExStyle { get; set; }
    
    /// <summary>元のウィンドウ位置・サイズ（復元用）</summary>
    public NativeMethods.RECT OriginalRect { get; set; }
    
    /// <summary>Ghost Mode 有効時の透明度（0-255）</summary>
    public byte CurrentGhostOpacity { get; set; } = 255;
    
    public WindowInfo(IntPtr handle)
    {
        Handle = handle;
    }
    
    /// <summary>何らかの変更が適用されているか</summary>
    public bool HasModifications => OpacityPercent < 100 || IsBorderless || IsGhostMode;
}

/// <summary>
/// ウィンドウの透明化・スタイル変更を管理するクラス
/// </summary>
public class WindowManager
{
    private readonly Dictionary<IntPtr, WindowInfo> _windows = [];
    
    /// <summary>除外ウィンドウのクラス名</summary>
    private static readonly HashSet<string> ExcludedClassNames =
    [
        "Shell_TrayWnd",           // タスクバー
        "Shell_SecondaryTrayWnd",  // セカンダリモニターのタスクバー
        "Windows.UI.Core.CoreWindow", // スタートメニュー等
        "Progman",                 // デスクトップ
        "WorkerW"                  // デスクトップ背景
    ];
    
    private readonly IntPtr _shellWindow;
    private readonly IntPtr _ownHandle;
    
    public WindowManager(IntPtr ownHandle)
    {
        _ownHandle = ownHandle;
        _shellWindow = NativeMethods.GetShellWindow();
    }
    
    /// <summary>変更が適用されているウィンドウの一覧</summary>
    public IEnumerable<WindowInfo> ModifiedWindows => _windows.Values.Where(w => w.HasModifications && NativeMethods.IsWindow(w.Handle));
    
    /// <summary>指定ウィンドウが操作対象外かどうか</summary>
    public bool IsExcluded(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return true;
        if (hwnd == _shellWindow) return true;
        if (hwnd == _ownHandle) return true;
        
        var className = new char[256];
        int length = NativeMethods.GetClassName(hwnd, className, 256);
        if (length > 0)
        {
            string name = new(className, 0, length);
            if (ExcludedClassNames.Contains(name))
                return true;
        }
        
        return false;
    }
    
    /// <summary>トップレベルウィンドウを取得</summary>
    public IntPtr GetTopLevelWindow(IntPtr hwnd)
    {
        IntPtr ancestor = NativeMethods.GetAncestor(hwnd, NativeMethods.GA_ROOTOWNER);
        return ancestor != IntPtr.Zero ? ancestor : hwnd;
    }
    
    /// <summary>ウィンドウ情報を取得（なければ作成）</summary>
    private WindowInfo GetOrCreateInfo(IntPtr hwnd)
    {
        if (!_windows.TryGetValue(hwnd, out var info))
        {
            info = new WindowInfo(hwnd)
            {
                OriginalStyle = NativeMethods.GetWindowLongW(hwnd, NativeMethods.GWL_STYLE),
                OriginalExStyle = NativeMethods.GetWindowLongW(hwnd, NativeMethods.GWL_EXSTYLE)
            };
            NativeMethods.GetWindowRect(hwnd, out var rect);
            info.OriginalRect = rect;
            _windows[hwnd] = info;
        }
        return info;
    }
    
    /// <summary>ウィンドウ情報を取得（存在する場合のみ）</summary>
    public WindowInfo? GetInfo(IntPtr hwnd)
    {
        return _windows.TryGetValue(hwnd, out var info) ? info : null;
    }
    
    /// <summary>透明度を変更（5%刻み）</summary>
    /// <param name="hwnd">ウィンドウハンドル</param>
    /// <param name="delta">変更量（正:不透明に、負:透明に）</param>
    public void ChangeOpacity(IntPtr hwnd, int delta)
    {
        if (IsExcluded(hwnd)) return;
        if (!NativeMethods.IsWindow(hwnd)) return;
        
        var info = GetOrCreateInfo(hwnd);
        int newOpacity = Math.Clamp(info.OpacityPercent + delta, 5, 100);
        info.OpacityPercent = (byte)newOpacity;
        
        ApplyOpacity(hwnd, info.OpacityPercent);
    }
    
    /// <summary>透明度を適用</summary>
    private static void ApplyOpacity(IntPtr hwnd, byte percent)
    {
        // WS_EX_LAYEREDを設定
        int exStyle = NativeMethods.GetWindowLongW(hwnd, NativeMethods.GWL_EXSTYLE);
        if ((exStyle & (int)NativeMethods.WS_EX_LAYERED) == 0)
        {
            NativeMethods.SetWindowLongW(hwnd, NativeMethods.GWL_EXSTYLE, exStyle | (int)NativeMethods.WS_EX_LAYERED);
        }
        
        byte alpha = (byte)(percent * 255 / 100);
        NativeMethods.SetLayeredWindowAttributes(hwnd, 0, alpha, NativeMethods.LWA_ALPHA);
    }
    
    /// <summary>直接透明度を設定（Ghost Mode用）</summary>
    public void SetDirectOpacity(IntPtr hwnd, byte alpha)
    {
        if (!NativeMethods.IsWindow(hwnd)) return;
        
        int exStyle = NativeMethods.GetWindowLongW(hwnd, NativeMethods.GWL_EXSTYLE);
        if ((exStyle & (int)NativeMethods.WS_EX_LAYERED) == 0)
        {
            NativeMethods.SetWindowLongW(hwnd, NativeMethods.GWL_EXSTYLE, exStyle | (int)NativeMethods.WS_EX_LAYERED);
        }
        
        NativeMethods.SetLayeredWindowAttributes(hwnd, 0, alpha, NativeMethods.LWA_ALPHA);
        
        var info = GetInfo(hwnd);
        if (info != null)
        {
            info.CurrentGhostOpacity = alpha;
        }
    }
    
    /// <summary>ボーダーレスモードをトグル</summary>
    public void ToggleBorderless(IntPtr hwnd)
    {
        if (IsExcluded(hwnd)) return;
        if (!NativeMethods.IsWindow(hwnd)) return;
        
        var info = GetOrCreateInfo(hwnd);
        
        if (info.IsBorderless)
        {
            // 復元
            RestoreStyle(hwnd, info);
        }
        else
        {
            // ボーダーレス化
            ApplyBorderless(hwnd, info);
        }
        
        info.IsBorderless = !info.IsBorderless;
    }
    
    /// <summary>ボーダーレススタイルを適用</summary>
    private static void ApplyBorderless(IntPtr hwnd, WindowInfo info)
    {
        // 現在の位置を保存
        NativeMethods.GetWindowRect(hwnd, out var rect);
        info.OriginalRect = rect;
        
        // スタイルからキャプション・枠を削除
        int style = NativeMethods.GetWindowLongW(hwnd, NativeMethods.GWL_STYLE);
        info.OriginalStyle = style;
        
        int newStyle = style & ~(int)(NativeMethods.WS_CAPTION | NativeMethods.WS_THICKFRAME);
        NativeMethods.SetWindowLongW(hwnd, NativeMethods.GWL_STYLE, newStyle);
        
        // 最大化状態の場合はフルスクリーン化
        if (NativeMethods.IsZoomed(hwnd))
        {
            // モニター情報を取得
            IntPtr hMonitor = NativeMethods.MonitorFromWindow(hwnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
            var mi = new NativeMethods.MONITORINFO { cbSize = Marshal.SizeOf<NativeMethods.MONITORINFO>() };
            if (NativeMethods.GetMonitorInfoW(hMonitor, ref mi))
            {
                // モニター全体に拡張（タスクバーも隠す）
                NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_TOP,
                    mi.rcMonitor.Left, mi.rcMonitor.Top,
                    mi.rcMonitor.Width, mi.rcMonitor.Height,
                    NativeMethods.SWP_FRAMECHANGED | NativeMethods.SWP_NOACTIVATE);
            }
        }
        else
        {
            // フレーム変更を反映
            NativeMethods.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER |
                NativeMethods.SWP_FRAMECHANGED | NativeMethods.SWP_NOACTIVATE);
        }
    }
    
    /// <summary>標準スタイルに復元</summary>
    private static void RestoreStyle(IntPtr hwnd, WindowInfo info)
    {
        // 元のスタイルを復元
        NativeMethods.SetWindowLongW(hwnd, NativeMethods.GWL_STYLE, info.OriginalStyle);
        
        // 元のサイズ・位置に復元
        NativeMethods.SetWindowPos(hwnd, IntPtr.Zero,
            info.OriginalRect.Left, info.OriginalRect.Top,
            info.OriginalRect.Width, info.OriginalRect.Height,
            NativeMethods.SWP_NOZORDER | NativeMethods.SWP_FRAMECHANGED | NativeMethods.SWP_NOACTIVATE);
    }
    
    /// <summary>Ghost Mode をトグル</summary>
    public void ToggleGhostMode(IntPtr hwnd)
    {
        if (IsExcluded(hwnd)) return;
        if (!NativeMethods.IsWindow(hwnd)) return;
        
        var info = GetOrCreateInfo(hwnd);
        info.IsGhostMode = !info.IsGhostMode;
        
        if (!info.IsGhostMode)
        {
            // Ghost Mode OFF時は設定透明度に戻す
            ApplyOpacity(hwnd, info.OpacityPercent);
        }
    }
    
    /// <summary>全ウィンドウをリセット（緊急リセット）</summary>
    public void ResetAll()
    {
        foreach (var info in _windows.Values.ToList())
        {
            if (!NativeMethods.IsWindow(info.Handle)) continue;
            
            // 透明度を100%に
            ApplyOpacity(info.Handle, 100);
            
            // ボーダーレスを解除
            if (info.IsBorderless)
            {
                RestoreStyle(info.Handle, info);
            }
            
            // Ghost Mode OFF
            info.IsGhostMode = false;
            info.OpacityPercent = 100;
            info.IsBorderless = false;
        }
        
        _windows.Clear();
    }
    
    /// <summary>無効なウィンドウをクリーンアップ</summary>
    public void Cleanup()
    {
        var invalidHandles = _windows.Keys.Where(h => !NativeMethods.IsWindow(h)).ToList();
        foreach (var handle in invalidHandles)
        {
            _windows.Remove(handle);
        }
    }
}
