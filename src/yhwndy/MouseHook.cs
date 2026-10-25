using System.Runtime.InteropServices;

namespace yhwndy;

public enum DragMode
{
    None,
    Move,
    ResizeN,
    ResizeS,
    ResizeE,
    ResizeW,
    ResizeNW,
    ResizeNE,
    ResizeSW,
    ResizeSE
}

/// <summary>
/// Ctrl+drag to move or resize a window in Ghost Mode.
/// The drag is handed to the OS through WM_SYSCOMMAND, so the OS runs the move and resize loop.
/// </summary>
public class MouseHook : IDisposable
{
    private readonly WindowManager _windowManager;
    private IntPtr _hookId = IntPtr.Zero;
    private NativeMethods.LowLevelMouseProc? _proc;
    private bool _disposed;
    
    /// <summary>Width in px of the edge area where a drag resizes instead of moving.</summary>
    private const int EDGE_SIZE = 16;
    
    // SC_SIZE plus a WMSZ_* value picks the edge to resize.
    private const int SC_MOVE = 0xF010;
    private const int SC_SIZE = 0xF000;
    private const int WMSZ_LEFT = 1;
    private const int WMSZ_RIGHT = 2;
    private const int WMSZ_TOP = 3;
    private const int WMSZ_TOPLEFT = 4;
    private const int WMSZ_TOPRIGHT = 5;
    private const int WMSZ_BOTTOM = 6;
    private const int WMSZ_BOTTOMLEFT = 7;
    private const int WMSZ_BOTTOMRIGHT = 8;
    
    public MouseHook(WindowManager windowManager)
    {
        _windowManager = windowManager;
    }
    
    public bool IsActive => _hookId != IntPtr.Zero;

    public void Start()
    {
        if (IsActive) return;
        _proc = HookCallback;
        _hookId = NativeMethods.SetWindowsHookExW(
            NativeMethods.WH_MOUSE_LL,
            _proc,
            NativeMethods.GetModuleHandle(null),
            0);
    }
    
    public void Stop()
    {
        if (_hookId != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }
    
    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam.ToInt32() == NativeMethods.WM_LBUTTONDOWN)
        {
            if ((NativeMethods.GetAsyncKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0)
            {
                var hookStruct = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
                if (StartDrag(hookStruct.pt))
                {
                    return new IntPtr(1); // Swallow the click so the app doesn't receive it.
                }
            }
        }
        
        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }
    
    private bool StartDrag(NativeMethods.POINT pt)
    {
        IntPtr hwnd = NativeMethods.WindowFromPoint(pt);
        if (hwnd == IntPtr.Zero) return false;
        
        hwnd = _windowManager.GetTopLevelWindow(hwnd);
        
        if (_windowManager.IsExcluded(hwnd)) return false;

        // Only windows in Ghost Mode, so Ctrl+click keeps working in other windows.
        var info = _windowManager.GetInfo(hwnd);
        if (info == null || !info.IsGhostMode) return false;

        if (NativeMethods.IsZoomed(hwnd)) return false;
        
        if (!NativeMethods.GetWindowRect(hwnd, out var rect)) return false;
        
        DragMode mode = GetDragMode(pt, rect);
        if (mode == DragMode.None) return false;
        
        NativeMethods.ReleaseCapture();
        
        if (mode == DragMode.Move)
        {
            NativeMethods.SendMessageW(hwnd, NativeMethods.WM_SYSCOMMAND, (IntPtr)SC_MOVE, IntPtr.Zero);
        }
        else
        {
            int direction = mode switch
            {
                DragMode.ResizeN => WMSZ_TOP,
                DragMode.ResizeS => WMSZ_BOTTOM,
                DragMode.ResizeE => WMSZ_RIGHT,
                DragMode.ResizeW => WMSZ_LEFT,
                DragMode.ResizeNW => WMSZ_TOPLEFT,
                DragMode.ResizeNE => WMSZ_TOPRIGHT,
                DragMode.ResizeSW => WMSZ_BOTTOMLEFT,
                DragMode.ResizeSE => WMSZ_BOTTOMRIGHT,
                _ => 0
            };
            NativeMethods.SendMessageW(hwnd, NativeMethods.WM_SYSCOMMAND, (IntPtr)(SC_SIZE + direction), IntPtr.Zero);
        }
        
        return true;
    }
    
    private static DragMode GetDragMode(NativeMethods.POINT pt, NativeMethods.RECT rect)
    {
        bool nearLeft = pt.X < rect.Left + EDGE_SIZE;
        bool nearRight = pt.X > rect.Right - EDGE_SIZE;
        bool nearTop = pt.Y < rect.Top + EDGE_SIZE;
        bool nearBottom = pt.Y > rect.Bottom - EDGE_SIZE;
        
        // Corners first, then edges; anywhere else moves the window.
        if (nearLeft && nearTop) return DragMode.ResizeNW;
        if (nearRight && nearTop) return DragMode.ResizeNE;
        if (nearLeft && nearBottom) return DragMode.ResizeSW;
        if (nearRight && nearBottom) return DragMode.ResizeSE;
        
        if (nearLeft) return DragMode.ResizeW;
        if (nearRight) return DragMode.ResizeE;
        if (nearTop) return DragMode.ResizeN;
        if (nearBottom) return DragMode.ResizeS;
        
        return DragMode.Move;
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
