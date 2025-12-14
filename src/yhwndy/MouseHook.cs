using System.Runtime.InteropServices;

namespace yhwndy;

/// <summary>
/// ドラッグ操作の種類
/// </summary>
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
/// Ctrl+ドラッグによるウィンドウ移動/リサイズを管理
/// OS標準のWM_SYSCOMMANDを使用して信頼性を向上
/// </summary>
public class MouseHook : IDisposable
{
    private readonly WindowManager _windowManager;
    private IntPtr _hookId = IntPtr.Zero;
    private NativeMethods.LowLevelMouseProc? _proc;
    private bool _disposed;
    
    /// <summary>端の判定距離(px)</summary>
    private const int EDGE_SIZE = 16;
    
    // SC_SIZE の方向定数
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
    
    /// <summary>マウスフックを開始</summary>
    public void Start()
    {
        _proc = HookCallback;
        _hookId = NativeMethods.SetWindowsHookExW(
            NativeMethods.WH_MOUSE_LL,
            _proc,
            NativeMethods.GetModuleHandle(null),
            0);
    }
    
    /// <summary>マウスフックを停止</summary>
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
            // Ctrlキーが押されているか確認
            if ((NativeMethods.GetAsyncKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0)
            {
                var hookStruct = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
                if (StartDrag(hookStruct.pt))
                {
                    return new IntPtr(1); // 入力を消費
                }
            }
        }
        
        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }
    
    private bool StartDrag(NativeMethods.POINT pt)
    {
        // マウス位置のウィンドウを取得
        IntPtr hwnd = NativeMethods.WindowFromPoint(pt);
        if (hwnd == IntPtr.Zero) return false;
        
        // トップレベルウィンドウを取得
        hwnd = _windowManager.GetTopLevelWindow(hwnd);
        
        // 除外ウィンドウか確認
        if (_windowManager.IsExcluded(hwnd)) return false;

        // ゴーストモードのウィンドウのみ処理
        var info = _windowManager.GetInfo(hwnd);
        if (info == null || !info.IsGhostMode) return false;

        // 最大化ウィンドウは無視
        if (NativeMethods.IsZoomed(hwnd)) return false;
        
        // ウィンドウ矩形を取得
        if (!NativeMethods.GetWindowRect(hwnd, out var rect)) return false;
        
        // ドラッグモードを決定
        DragMode mode = GetDragMode(pt, rect);
        if (mode == DragMode.None) return false;
        
        // OS標準のドラッグを開始
        NativeMethods.ReleaseCapture();
        
        if (mode == DragMode.Move)
        {
            // 移動: SC_MOVE を送信
            NativeMethods.SendMessageW(hwnd, NativeMethods.WM_SYSCOMMAND, (IntPtr)SC_MOVE, IntPtr.Zero);
        }
        else
        {
            // リサイズ: SC_SIZE + 方向 を送信
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
    
    /// <summary>マウス位置からドラッグモードを決定</summary>
    private static DragMode GetDragMode(NativeMethods.POINT pt, NativeMethods.RECT rect)
    {
        bool nearLeft = pt.X < rect.Left + EDGE_SIZE;
        bool nearRight = pt.X > rect.Right - EDGE_SIZE;
        bool nearTop = pt.Y < rect.Top + EDGE_SIZE;
        bool nearBottom = pt.Y > rect.Bottom - EDGE_SIZE;
        
        // 角
        if (nearLeft && nearTop) return DragMode.ResizeNW;
        if (nearRight && nearTop) return DragMode.ResizeNE;
        if (nearLeft && nearBottom) return DragMode.ResizeSW;
        if (nearRight && nearBottom) return DragMode.ResizeSE;
        
        // 辺
        if (nearLeft) return DragMode.ResizeW;
        if (nearRight) return DragMode.ResizeE;
        if (nearTop) return DragMode.ResizeN;
        if (nearBottom) return DragMode.ResizeS;
        
        // 中央（移動）
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
