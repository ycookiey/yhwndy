namespace yhwndy;

/// <summary>
/// ホットキーの登録・処理を管理するクラス
/// </summary>
public class HotkeyManager : IDisposable
{
    private readonly IntPtr _hwnd;
    private readonly List<int> _registeredIds = [];
    private bool _disposed;
    
    // ホットキーID
    public const int HOTKEY_OPACITY_UP = 1;
    public const int HOTKEY_OPACITY_DOWN = 2;
    public const int HOTKEY_BORDERLESS = 3;
    public const int HOTKEY_GHOST_MODE = 4;
    
    /// <summary>登録に失敗したホットキーがあったか</summary>
    public bool HasFailedRegistration { get; private set; }
    
    public HotkeyManager(IntPtr hwnd)
    {
        _hwnd = hwnd;
    }
    
    /// <summary>全ホットキーを登録</summary>
    public void RegisterAll()
    {
        // Ctrl+Alt+↑: 透明度UP
        if (!Register(HOTKEY_OPACITY_UP, NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT | NativeMethods.MOD_NOREPEAT, NativeMethods.VK_UP))
            HasFailedRegistration = true;
        
        // Ctrl+Alt+↓: 透明度DOWN
        if (!Register(HOTKEY_OPACITY_DOWN, NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT | NativeMethods.MOD_NOREPEAT, NativeMethods.VK_DOWN))
            HasFailedRegistration = true;
        
        // Ctrl+Alt+B: ボーダーレストグル
        if (!Register(HOTKEY_BORDERLESS, NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT | NativeMethods.MOD_NOREPEAT, (int)'B'))
            HasFailedRegistration = true;
        
        // Ctrl+Alt+G: Ghost Mode トグル
        if (!Register(HOTKEY_GHOST_MODE, NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT | NativeMethods.MOD_NOREPEAT, (int)'G'))
            HasFailedRegistration = true;
    }
    
    private bool Register(int id, uint modifiers, int vk)
    {
        bool success = NativeMethods.RegisterHotKey(_hwnd, id, modifiers, vk);
        if (success)
        {
            _registeredIds.Add(id);
        }
        return success;
    }
    
    /// <summary>全ホットキーを解除</summary>
    public void UnregisterAll()
    {
        foreach (var id in _registeredIds)
        {
            NativeMethods.UnregisterHotKey(_hwnd, id);
        }
        _registeredIds.Clear();
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            UnregisterAll();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
