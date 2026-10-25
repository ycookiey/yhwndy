namespace yhwndy;

/// <summary>
/// Fades the windows in Ghost Mode by their distance from the mouse cursor.
/// The timer runs only while a window is in Ghost Mode, and the mouse hook for
/// Ctrl+drag is set only while Ctrl is held: a low-level hook makes every mouse
/// input in every app wait for this process.
/// </summary>
public class GhostModeManager : IDisposable
{
    private readonly WindowManager _windowManager;
    private readonly MouseHook _mouseHook;
    private readonly System.Windows.Forms.Timer _timer;
    private bool _disposed;
    
    /// <summary>Distance in px below which the window is at its lowest opacity.</summary>
    private const double DISTANCE_TRANSPARENT = 50;
    
    /// <summary>Distance in px from which the window is at its set opacity.</summary>
    private const double DISTANCE_NORMAL = 150;
    
    public GhostModeManager(WindowManager windowManager, MouseHook mouseHook)
    {
        _windowManager = windowManager;
        _mouseHook = mouseHook;

        _timer = new System.Windows.Forms.Timer
        {
            Interval = 100
        };
        _timer.Tick += OnTimerTick;
    }

    /// <summary>
    /// Starts the timer if a window is in Ghost Mode, or stops the timer and the
    /// mouse hook if none is. Call after Ghost Mode is turned on or off.
    /// </summary>
    public void Refresh()
    {
        if (GhostWindows().Any())
        {
            _timer.Start();
        }
        else
        {
            _timer.Stop();
            _mouseHook.Stop();
        }
    }

    private IEnumerable<WindowInfo> GhostWindows() =>
        _windowManager.ModifiedWindows.Where(w => w.IsGhostMode);

    private void OnTimerTick(object? sender, EventArgs e)
    {
        var ghosts = GhostWindows().ToList();
        if (ghosts.Count == 0)
        {
            // The last window in Ghost Mode was closed.
            Refresh();
            return;
        }

        bool ctrlPressed = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0;

        // Ctrl+drag starts with a click while Ctrl is held, so the hook is needed
        // only then. A click within one tick of pressing Ctrl may not start a drag.
        if (ctrlPressed)
        {
            _mouseHook.Start();
        }
        else
        {
            _mouseHook.Stop();
        }

        if (!NativeMethods.GetCursorPos(out var mousePos)) return;

        foreach (var info in ghosts)
        {
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
