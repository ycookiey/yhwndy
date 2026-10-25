using Microsoft.Win32;

namespace yhwndy;

/// <summary>
/// Starts yhwndy at sign-in through the HKCU Run key.
/// </summary>
public static class StartupManager
{
    private const string RegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "yhwndy";
    
    public static bool IsRegistered
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, false);
            return key?.GetValue(AppName) != null;
        }
    }
    
    public static void Toggle()
    {
        if (IsRegistered)
        {
            Unregister();
        }
        else
        {
            Register();
        }
    }
    
    public static void Register()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
        if (key != null)
        {
            string exePath = Environment.ProcessPath ?? Application.ExecutablePath;
            key.SetValue(AppName, $"\"{exePath}\"");
        }
    }
    
    public static void Unregister()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
        key?.DeleteValue(AppName, false);
    }
}
