using System.Runtime.InteropServices;

namespace yhwndy;

/// <summary>
/// State that yhwndy keeps for one window while it runs.
/// </summary>
public class WindowInfo
{
    public IntPtr Handle { get; }
    
    /// <summary>Set opacity in percent (5-100), applied as alpha 0-255.</summary>
    public byte OpacityPercent { get; set; } = 100;
    
    public bool IsBorderless { get; set; }
    
    public bool IsGhostMode { get; set; }
    
    /// <summary>Style before borderless, restored when borderless is turned off.</summary>
    public int OriginalStyle { get; set; }
    
    // TODO: restore this on reset; resetting leaves WS_EX_LAYERED set.
    /// <summary>Extended style when yhwndy first changed the window.</summary>
    public int OriginalExStyle { get; set; }
    
    /// <summary>Position and size before borderless, restored when borderless is turned off.</summary>
    public NativeMethods.RECT OriginalRect { get; set; }
    
    /// <summary>Alpha last set by Ghost Mode, to skip redundant updates.</summary>
    public byte CurrentGhostOpacity { get; set; } = 255;
    
    public WindowInfo(IntPtr handle)
    {
        Handle = handle;
    }
    
    public bool HasModifications => OpacityPercent < 100 || IsBorderless || IsGhostMode;
}

/// <summary>
/// Applies and restores transparency, borderless and Ghost Mode per window.
/// </summary>
public class WindowManager
{
    private readonly Dictionary<IntPtr, WindowInfo> _windows = [];
    
    /// <summary>Window classes of the shell that yhwndy never changes.</summary>
    private static readonly HashSet<string> ExcludedClassNames =
    [
        "Shell_TrayWnd",           // Taskbar
        "Shell_SecondaryTrayWnd",  // Taskbar on the other monitors
        "Windows.UI.Core.CoreWindow", // Start menu and other shell UI
        "Progman",                 // Desktop
        "WorkerW"                  // Desktop background
    ];
    
    private readonly IntPtr _shellWindow;
    private readonly IntPtr _ownHandle;
    
    public WindowManager(IntPtr ownHandle)
    {
        _ownHandle = ownHandle;
        _shellWindow = NativeMethods.GetShellWindow();
    }
    
    /// <summary>Windows that yhwndy has changed and that still exist.</summary>
    public IEnumerable<WindowInfo> ModifiedWindows => _windows.Values.Where(w => w.HasModifications && NativeMethods.IsWindow(w.Handle));
    
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
    
    /// <summary>Hotkeys and Ctrl+drag act on the root owner, not on a child or owned window.</summary>
    public IntPtr GetTopLevelWindow(IntPtr hwnd)
    {
        IntPtr ancestor = NativeMethods.GetAncestor(hwnd, NativeMethods.GA_ROOTOWNER);
        return ancestor != IntPtr.Zero ? ancestor : hwnd;
    }
    
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
    
    public WindowInfo? GetInfo(IntPtr hwnd)
    {
        return _windows.TryGetValue(hwnd, out var info) ? info : null;
    }
    
    /// <param name="delta">Percent to add; a negative value makes the window more transparent.</param>
    public void ChangeOpacity(IntPtr hwnd, int delta)
    {
        if (IsExcluded(hwnd)) return;
        if (!NativeMethods.IsWindow(hwnd)) return;
        
        var info = GetOrCreateInfo(hwnd);
        int newOpacity = Math.Clamp(info.OpacityPercent + delta, 5, 100);
        info.OpacityPercent = (byte)newOpacity;
        
        ApplyOpacity(hwnd, info.OpacityPercent);
    }
    
    private static void ApplyOpacity(IntPtr hwnd, byte percent)
    {
        // SetLayeredWindowAttributes works only on WS_EX_LAYERED windows.
        int exStyle = NativeMethods.GetWindowLongW(hwnd, NativeMethods.GWL_EXSTYLE);
        if ((exStyle & (int)NativeMethods.WS_EX_LAYERED) == 0)
        {
            NativeMethods.SetWindowLongW(hwnd, NativeMethods.GWL_EXSTYLE, exStyle | (int)NativeMethods.WS_EX_LAYERED);
        }
        
        byte alpha = (byte)(percent * 255 / 100);
        NativeMethods.SetLayeredWindowAttributes(hwnd, 0, alpha, NativeMethods.LWA_ALPHA);
    }
    
    /// <summary>Sets the alpha without changing the set opacity. Used by Ghost Mode.</summary>
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
    
    public void ToggleBorderless(IntPtr hwnd)
    {
        if (IsExcluded(hwnd)) return;
        if (!NativeMethods.IsWindow(hwnd)) return;
        
        var info = GetOrCreateInfo(hwnd);
        
        if (info.IsBorderless)
        {
            RestoreStyle(hwnd, info);
        }
        else
        {
            ApplyBorderless(hwnd, info);
        }
        
        info.IsBorderless = !info.IsBorderless;
    }
    
    private static void ApplyBorderless(IntPtr hwnd, WindowInfo info)
    {
        NativeMethods.GetWindowRect(hwnd, out var rect);
        info.OriginalRect = rect;
        
        int style = NativeMethods.GetWindowLongW(hwnd, NativeMethods.GWL_STYLE);
        info.OriginalStyle = style;
        
        int newStyle = style & ~(int)(NativeMethods.WS_CAPTION | NativeMethods.WS_THICKFRAME);
        NativeMethods.SetWindowLongW(hwnd, NativeMethods.GWL_STYLE, newStyle);
        
        if (NativeMethods.IsZoomed(hwnd))
        {
            IntPtr hMonitor = NativeMethods.MonitorFromWindow(hwnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
            var mi = new NativeMethods.MONITORINFO { cbSize = Marshal.SizeOf<NativeMethods.MONITORINFO>() };
            if (NativeMethods.GetMonitorInfoW(hMonitor, ref mi))
            {
                // rcMonitor, not rcWork, so the window covers the taskbar.
                NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_TOP,
                    mi.rcMonitor.Left, mi.rcMonitor.Top,
                    mi.rcMonitor.Width, mi.rcMonitor.Height,
                    NativeMethods.SWP_FRAMECHANGED | NativeMethods.SWP_NOACTIVATE);
            }
        }
        else
        {
            // SWP_FRAMECHANGED makes the new style take effect.
            NativeMethods.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER |
                NativeMethods.SWP_FRAMECHANGED | NativeMethods.SWP_NOACTIVATE);
        }
    }
    
    private static void RestoreStyle(IntPtr hwnd, WindowInfo info)
    {
        NativeMethods.SetWindowLongW(hwnd, NativeMethods.GWL_STYLE, info.OriginalStyle);
        
        NativeMethods.SetWindowPos(hwnd, IntPtr.Zero,
            info.OriginalRect.Left, info.OriginalRect.Top,
            info.OriginalRect.Width, info.OriginalRect.Height,
            NativeMethods.SWP_NOZORDER | NativeMethods.SWP_FRAMECHANGED | NativeMethods.SWP_NOACTIVATE);
    }
    
    public void ToggleGhostMode(IntPtr hwnd)
    {
        if (IsExcluded(hwnd)) return;
        if (!NativeMethods.IsWindow(hwnd)) return;
        
        var info = GetOrCreateInfo(hwnd);
        info.IsGhostMode = !info.IsGhostMode;
        
        if (!info.IsGhostMode)
        {
            ApplyOpacity(hwnd, info.OpacityPercent);
        }
    }
    
    /// <summary>Restores every changed window. Used by the reset menu item and on exit.</summary>
    public void ResetAll()
    {
        foreach (var info in _windows.Values.ToList())
        {
            if (!NativeMethods.IsWindow(info.Handle)) continue;
            
            ApplyOpacity(info.Handle, 100);
            
            if (info.IsBorderless)
            {
                RestoreStyle(info.Handle, info);
            }
            
            info.IsGhostMode = false;
            info.OpacityPercent = 100;
            info.IsBorderless = false;
        }
        
        _windows.Clear();
    }
    
    /// <summary>Removes the entries of windows that no longer exist.</summary>
    public void Cleanup()
    {
        var invalidHandles = _windows.Keys.Where(h => !NativeMethods.IsWindow(h)).ToList();
        foreach (var handle in invalidHandles)
        {
            _windows.Remove(handle);
        }
    }
}
