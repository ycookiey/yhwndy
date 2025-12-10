namespace yhwndy;

/// <summary>
/// Ghost Mode（マウス距離に応じた透明度自動制御）を管理するクラス
/// </summary>
public class GhostModeManager : IDisposable
{
    private readonly WindowManager _windowManager;
    private readonly System.Windows.Forms.Timer _timer;
    private bool _disposed;
    
    /// <summary>完全透明になる距離(px)</summary>
    private const double DISTANCE_TRANSPARENT = 50;
    
    /// <summary>通常表示になる距離(px)</summary>
    private const double DISTANCE_NORMAL = 150;
    
    public GhostModeManager(WindowManager windowManager)
    {
        _windowManager = windowManager;
        
        _timer = new System.Windows.Forms.Timer
        {
            Interval = 100 // 100ms間隔
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }
    
    private void OnTimerTick(object? sender, EventArgs e)
    {
        // Ctrlキーが押されているか確認
        bool ctrlPressed = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0;
        
        // マウス位置を取得
        if (!NativeMethods.GetCursorPos(out var mousePos)) return;
        
        // Ghost Mode が有効なウィンドウを更新
        foreach (var info in _windowManager.ModifiedWindows)
        {
            if (!info.IsGhostMode) continue;
            if (!NativeMethods.IsWindow(info.Handle)) continue;
            
            // Ctrl押下中は設定透明度で固定
            if (ctrlPressed)
            {
                byte alpha = (byte)(info.OpacityPercent * 255 / 100);
                if (info.CurrentGhostOpacity != alpha)
                {
                    _windowManager.SetDirectOpacity(info.Handle, alpha);
                }
                continue;
            }
            
            // ウィンドウからの距離を計算
            if (!NativeMethods.GetWindowRect(info.Handle, out var rect)) continue;
            
            double distance = GetDistanceFromRect(mousePos.X, mousePos.Y, rect);
            byte newAlpha = CalculateOpacity(distance, info.OpacityPercent);
            
            if (info.CurrentGhostOpacity != newAlpha)
            {
                _windowManager.SetDirectOpacity(info.Handle, newAlpha);
            }
        }
    }
    
    /// <summary>
    /// 矩形の端からの最短距離を計算（ウィンドウ内部は0として扱う）
    /// </summary>
    private static double GetDistanceFromRect(int x, int y, NativeMethods.RECT rect)
    {
        // ウィンドウ内部の場合は0
        if (x >= rect.Left && x <= rect.Right && y >= rect.Top && y <= rect.Bottom)
        {
            return 0;
        }
        
        // 最も近い点を計算
        int nearestX = Math.Max(rect.Left, Math.Min(x, rect.Right));
        int nearestY = Math.Max(rect.Top, Math.Min(y, rect.Bottom));
        
        double dx = x - nearestX;
        double dy = y - nearestY;
        
        return Math.Sqrt(dx * dx + dy * dy);
    }
    
    /// <summary>
    /// 距離に応じた透明度を計算
    /// </summary>
    /// <param name="distance">ウィンドウからの距離(px)</param>
    /// <param name="maxOpacityPercent">設定透明度(%)</param>
    /// <returns>透明度(0-255)</returns>
    private static byte CalculateOpacity(double distance, byte maxOpacityPercent)
    {
        if (distance < DISTANCE_TRANSPARENT)
        {
            // 50px未満: 完全透明（ただし最小限の可視性を維持するため13=5%程度）
            return 13; // 約5%
        }
        
        if (distance >= DISTANCE_NORMAL)
        {
            // 150px以上: 設定透明度で表示
            return (byte)(maxOpacityPercent * 255 / 100);
        }
        
        // 50-150px: 線形補間
        double ratio = (distance - DISTANCE_TRANSPARENT) / (DISTANCE_NORMAL - DISTANCE_TRANSPARENT);
        byte maxAlpha = (byte)(maxOpacityPercent * 255 / 100);
        return (byte)(13 + (maxAlpha - 13) * ratio);
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _timer.Stop();
            _timer.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
