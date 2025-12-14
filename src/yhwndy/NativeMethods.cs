using System.Runtime.InteropServices;

namespace yhwndy;

/// <summary>
/// Win32 API P/Invoke definitions
/// </summary>
public static partial class NativeMethods
{
    #region Window Style Constants
    
    public const int GWL_STYLE = -16;
    public const int GWL_EXSTYLE = -20;
    
    public const uint WS_CAPTION = 0x00C00000;
    public const uint WS_THICKFRAME = 0x00040000;
    public const uint WS_MINIMIZEBOX = 0x00020000;
    public const uint WS_MAXIMIZEBOX = 0x00010000;
    public const uint WS_SYSMENU = 0x00080000;
    
    public const uint WS_EX_LAYERED = 0x00080000;
    public const uint WS_EX_TRANSPARENT = 0x00000020;
    public const uint WS_EX_TOOLWINDOW = 0x00000080;
    
    public const byte LWA_ALPHA = 0x02;
    
    #endregion
    
    #region Window Position Constants
    
    public static readonly IntPtr HWND_TOPMOST = new(-1);
    public static readonly IntPtr HWND_NOTOPMOST = new(-2);
    public static readonly IntPtr HWND_TOP = IntPtr.Zero;
    
    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOMOVE = 0x0002;
    public const uint SWP_NOZORDER = 0x0004;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_FRAMECHANGED = 0x0020;
    public const uint SWP_SHOWWINDOW = 0x0040;
    
    #endregion
    
    #region Hotkey Constants
    
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;
    public const uint MOD_NOREPEAT = 0x4000;
    
    public const int WM_HOTKEY = 0x0312;
    
    public const int VK_UP = 0x26;
    public const int VK_DOWN = 0x28;
    public const int VK_CONTROL = 0x11;
    public const int VK_LBUTTON = 0x01;
    
    #endregion
    
    #region Mouse Hook Constants
    
    public const int WH_MOUSE_LL = 14;
    public const int WM_LBUTTONDOWN = 0x0201;
    public const int WM_LBUTTONUP = 0x0202;
    public const int WM_MOUSEMOVE = 0x0200;
    
    #endregion
    
    #region Cursor Constants
    
    public const int IDC_ARROW = 32512;
    public const int IDC_SIZEALL = 32646;
    public const int IDC_SIZENS = 32645;
    public const int IDC_SIZEWE = 32644;
    public const int IDC_SIZENWSE = 32642;
    public const int IDC_SIZENESW = 32643;
    
    #endregion
    
    #region Structures
    
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
        
        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }
    
    #endregion
    
    #region Delegates
    
    public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
    
    #endregion
    
    #region Window Functions
    
    [LibraryImport("user32.dll")]
    public static partial IntPtr GetForegroundWindow();
    
    [LibraryImport("user32.dll")]
    public static partial IntPtr GetShellWindow();
    
    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial int GetWindowLongW(IntPtr hWnd, int nIndex);
    
    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial int SetWindowLongW(IntPtr hWnd, int nIndex, int dwNewLong);
    
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);
    
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsZoomed(IntPtr hWnd);
    
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsIconic(IntPtr hWnd);
    
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsWindow(IntPtr hWnd);
    
    [LibraryImport("user32.dll", EntryPoint = "GetClassNameW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    public static partial int GetClassName(IntPtr hWnd, [Out] char[] lpClassName, int nMaxCount);
    
    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);
    
    [LibraryImport("user32.dll")]
    public static partial IntPtr WindowFromPoint(POINT Point);
    
    public const uint GA_ROOTOWNER = 3;
    
    #endregion
    
    #region Hotkey Functions
    
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, int vk);
    
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UnregisterHotKey(IntPtr hWnd, int id);
    
    [LibraryImport("user32.dll")]
    public static partial short GetAsyncKeyState(int vKey);
    
    #endregion
    
    #region Hook Functions
    
    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial IntPtr SetWindowsHookExW(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);
    
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UnhookWindowsHookEx(IntPtr hhk);
    
    [LibraryImport("user32.dll")]
    public static partial IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    
    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial IntPtr GetModuleHandle(string? lpModuleName);
    
    #endregion
    
    #region Cursor Functions
    
    [LibraryImport("user32.dll")]
    public static partial IntPtr LoadCursorW(IntPtr hInstance, IntPtr lpCursorName);
    
    [LibraryImport("user32.dll")]
    public static partial IntPtr SetCursor(IntPtr hCursor);
    
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetCursorPos(out POINT lpPoint);
    
    #endregion
    
    #region Monitor Functions
    
    [LibraryImport("user32.dll")]
    public static partial IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);
    
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetMonitorInfoW(IntPtr hMonitor, ref MONITORINFO lpmi);
    
    public const uint MONITOR_DEFAULTTONEAREST = 0x00000002;
    
    #endregion
    
    #region Process Functions
    
    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    
    #endregion
    
    #region Message Functions
    
    public const int WM_SYSCOMMAND = 0x0112;
    
    [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
    public static partial IntPtr SendMessageW(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
    
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ReleaseCapture();
    
    #endregion
}
