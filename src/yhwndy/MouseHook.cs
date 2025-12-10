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
/// </summary>
public class MouseHook : IDisposable
{
    private readonly WindowManager _windowManager;
    private IntPtr _hookId = IntPtr.Zero;
    private NativeMethods.LowLevelMouseProc? _proc;
    private bool _disposed;
    
    /// <summary>端の判定距離(px)</summary>
    private const int EDGE_SIZE = 16;
    
    // ドラッグ状態
    private bool _isDragging;
    private DragMode _dragMode = DragMode.None;
    private IntPtr _dragWindow = IntPtr.Zero;
    private NativeMethods.POINT _dragStart;
    private NativeMethods.RECT _originalRect;
    private double _aspectRatio;
    
    // 元のカーソル
    private IntPtr _originalCursor = IntPtr.Zero;
    
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
        if (nCode >= 0)
        {
            int msg = wParam.ToInt32();
            var hookStruct = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
            
            switch (msg)
            {
                case NativeMethods.WM_LBUTTONDOWN:
                    if (OnMouseDown(hookStruct.pt))
                        return new IntPtr(1); // 入力を消費
                    break;
                    
                case NativeMethods.WM_MOUSEMOVE:
                    if (_isDragging)
                    {
                        OnMouseMove(hookStruct.pt);
                        return new IntPtr(1);
                    }
                    break;
                    
                case NativeMethods.WM_LBUTTONUP:
                    if (_isDragging)
                    {
                        OnMouseUp();
                        return new IntPtr(1);
                    }
                    break;
            }
        }
        
        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }
    
    private bool OnMouseDown(NativeMethods.POINT pt)
    {
        // Ctrlキーが押されているか確認
        if ((NativeMethods.GetAsyncKeyState(NativeMethods.VK_CONTROL) & 0x8000) == 0)
            return false;
        
        // アクティブウィンドウを取得
        IntPtr hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return false;
        
        // トップレベルウィンドウを取得
        hwnd = _windowManager.GetTopLevelWindow(hwnd);
        
        // 除外ウィンドウか確認
        if (_windowManager.IsExcluded(hwnd)) return false;
        
        // 最大化ウィンドウは無視
        if (NativeMethods.IsZoomed(hwnd)) return false;
        
        // ウィンドウ矩形を取得
        if (!NativeMethods.GetWindowRect(hwnd, out var rect)) return false;
        
        // マウス位置がウィンドウ内か確認
        if (pt.X < rect.Left || pt.X > rect.Right || pt.Y < rect.Top || pt.Y > rect.Bottom)
            return false;
        
        // ドラッグモードを決定
        _dragMode = GetDragMode(pt, rect);
        if (_dragMode == DragMode.None) return false;
        
        // ドラッグ開始
        _isDragging = true;
        _dragWindow = hwnd;
        _dragStart = pt;
        _originalRect = rect;
        _aspectRatio = rect.Width / (double)rect.Height;
        
        // カーソルを変更
        SetDragCursor(_dragMode);
        
        return true;
    }
    
    private void OnMouseMove(NativeMethods.POINT pt)
    {
        if (!_isDragging || _dragWindow == IntPtr.Zero) return;
        
        int dx = pt.X - _dragStart.X;
        int dy = pt.Y - _dragStart.Y;
        
        int newLeft = _originalRect.Left;
        int newTop = _originalRect.Top;
        int newWidth = _originalRect.Width;
        int newHeight = _originalRect.Height;
        
        switch (_dragMode)
        {
            case DragMode.Move:
                newLeft += dx;
                newTop += dy;
                break;
                
            case DragMode.ResizeE:
                newWidth += dx;
                break;
                
            case DragMode.ResizeW:
                newLeft += dx;
                newWidth -= dx;
                break;
                
            case DragMode.ResizeS:
                newHeight += dy;
                break;
                
            case DragMode.ResizeN:
                newTop += dy;
                newHeight -= dy;
                break;
                
            case DragMode.ResizeSE:
                // アスペクト比維持
                if (Math.Abs(dx) > Math.Abs(dy))
                {
                    newWidth += dx;
                    newHeight = (int)(newWidth / _aspectRatio);
                }
                else
                {
                    newHeight += dy;
                    newWidth = (int)(newHeight * _aspectRatio);
                }
                break;
                
            case DragMode.ResizeSW:
                if (Math.Abs(dx) > Math.Abs(dy))
                {
                    newWidth -= dx;
                    newHeight = (int)(newWidth / _aspectRatio);
                    newLeft = _originalRect.Right - newWidth;
                }
                else
                {
                    newHeight += dy;
                    newWidth = (int)(newHeight * _aspectRatio);
                    newLeft = _originalRect.Right - newWidth;
                }
                break;
                
            case DragMode.ResizeNE:
                if (Math.Abs(dx) > Math.Abs(dy))
                {
                    newWidth += dx;
                    newHeight = (int)(newWidth / _aspectRatio);
                    newTop = _originalRect.Bottom - newHeight;
                }
                else
                {
                    newHeight -= dy;
                    newWidth = (int)(newHeight * _aspectRatio);
                    newTop = _originalRect.Bottom - newHeight;
                }
                break;
                
            case DragMode.ResizeNW:
                if (Math.Abs(dx) > Math.Abs(dy))
                {
                    newWidth -= dx;
                    newHeight = (int)(newWidth / _aspectRatio);
                    newLeft = _originalRect.Right - newWidth;
                    newTop = _originalRect.Bottom - newHeight;
                }
                else
                {
                    newHeight -= dy;
                    newWidth = (int)(newHeight * _aspectRatio);
                    newLeft = _originalRect.Right - newWidth;
                    newTop = _originalRect.Bottom - newHeight;
                }
                break;
        }
        
        // 最小サイズ制限
        if (newWidth < 50) newWidth = 50;
        if (newHeight < 50) newHeight = 50;
        
        NativeMethods.SetWindowPos(_dragWindow, IntPtr.Zero,
            newLeft, newTop, newWidth, newHeight,
            NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);
    }
    
    private void OnMouseUp()
    {
        _isDragging = false;
        _dragWindow = IntPtr.Zero;
        _dragMode = DragMode.None;
        
        // カーソルを復元
        RestoreCursor();
    }
    
    /// <summary>マウス位置からドラッグモードを決定</summary>
    private static DragMode GetDragMode(NativeMethods.POINT pt, NativeMethods.RECT rect)
    {
        bool nearLeft = pt.X < rect.Left + EDGE_SIZE;
        bool nearRight = pt.X > rect.Right - EDGE_SIZE;
        bool nearTop = pt.Y < rect.Top + EDGE_SIZE;
        bool nearBottom = pt.Y > rect.Bottom - EDGE_SIZE;
        
        // 角（アスペクト比維持）
        if (nearLeft && nearTop) return DragMode.ResizeNW;
        if (nearRight && nearTop) return DragMode.ResizeNE;
        if (nearLeft && nearBottom) return DragMode.ResizeSW;
        if (nearRight && nearBottom) return DragMode.ResizeSE;
        
        // 辺（アスペクト比非維持）
        if (nearLeft) return DragMode.ResizeW;
        if (nearRight) return DragMode.ResizeE;
        if (nearTop) return DragMode.ResizeN;
        if (nearBottom) return DragMode.ResizeS;
        
        // 中央（移動）
        return DragMode.Move;
    }
    
    private void SetDragCursor(DragMode mode)
    {
        IntPtr cursorId = mode switch
        {
            DragMode.Move => (IntPtr)NativeMethods.IDC_SIZEALL,
            DragMode.ResizeN or DragMode.ResizeS => (IntPtr)NativeMethods.IDC_SIZENS,
            DragMode.ResizeE or DragMode.ResizeW => (IntPtr)NativeMethods.IDC_SIZEWE,
            DragMode.ResizeNW or DragMode.ResizeSE => (IntPtr)NativeMethods.IDC_SIZENWSE,
            DragMode.ResizeNE or DragMode.ResizeSW => (IntPtr)NativeMethods.IDC_SIZENESW,
            _ => (IntPtr)NativeMethods.IDC_ARROW
        };
        
        IntPtr cursor = NativeMethods.LoadCursorW(IntPtr.Zero, cursorId);
        _originalCursor = NativeMethods.SetCursor(cursor);
    }
    
    private void RestoreCursor()
    {
        IntPtr arrow = NativeMethods.LoadCursorW(IntPtr.Zero, (IntPtr)NativeMethods.IDC_ARROW);
        NativeMethods.SetCursor(arrow);
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
