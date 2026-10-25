namespace yhwndy;

/// <summary>
/// Fades the windows in Ghost Mode by their distance from the mouse cursor.
/// </summary>
public class GhostModeManager : IDisposable
{
    private readonly WindowManager _windowManager;
    private readonly System.Windows.Forms.Timer _timer;
    private bool _disposed;
    
    /// <summary>Distance in px below which the window is at its lowest opacity.</summary>
    private const double DISTANCE_TRANSPARENT = 50;
    
    /// <summary>Distance in px from which the window is at its set opacity.</summary>
    private const double DISTANCE_NORMAL = 150;
    
    public GhostModeManager(WindowManager windowManager)
    {
        _windowManager = windowManager;
        
        _timer = new System.Windows.Forms.Timer
        {
            Interval = 100
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }
    
    private void OnTimerTick(object? sender, EventArgs e)
    {
        bool ctrlPressed = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0;
        
        if (!NativeMethods.GetCursorPos(out var mousePos)) return;
        
        foreach (var info in _windowManager.ModifiedWindows)
        {
            if (!info.IsGhostMode) continue;
            if (!NativeMethods.IsWindow(info.Handle)) continue;
            
            // Holding Ctrl shows the window at its set opacity, so it can be seen and Ctrl+dragged.
            if (ctrlPressed)
            {
                byte alpha = (byte)(info.OpacityPercent * 255 / 100);
                if (info.CurrentGhostOpacity != alpha)
                {
                    _windowManager.SetDirectOpacity(info.Handle, alpha);
                }
                continue;
            }
            
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
    /// Shortest distance from the point to the rectangle; 0 inside it.
    /// </summary>
    private static double GetDistanceFromRect(int x, int y, NativeMethods.RECT rect)
    {
        if (x >= rect.Left && x <= rect.Right && y >= rect.Top && y <= rect.Bottom)
        {
            return 0;
        }
        
        int nearestX = Math.Max(rect.Left, Math.Min(x, rect.Right));
        int nearestY = Math.Max(rect.Top, Math.Min(y, rect.Bottom));
        
        double dx = x - nearestX;
        double dy = y - nearestY;
        
        return Math.Sqrt(dx * dx + dy * dy);
    }
    
    /// <summary>
    /// Alpha for the given distance from the window.
    /// </summary>
    /// <param name="distance">Distance from the window in px.</param>
    /// <param name="maxOpacityPercent">Set opacity of the window in percent.</param>
    /// <returns>Alpha (0-255).</returns>
    private static byte CalculateOpacity(double distance, byte maxOpacityPercent)
    {
        if (distance < DISTANCE_TRANSPARENT)
        {
            // Not fully transparent: about 5% (13/255) keeps the window findable.
            return 13;
        }
        
        if (distance >= DISTANCE_NORMAL)
        {
            return (byte)(maxOpacityPercent * 255 / 100);
        }
        
        // Linear fade between the two distances.
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
