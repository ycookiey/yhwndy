using Microsoft.Win32;

namespace yhwndy;

/// <summary>
/// スタートアップ登録を管理するクラス
/// </summary>
public static class StartupManager
{
    private const string RegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "yhwndy";
    
    /// <summary>スタートアップに登録されているか</summary>
    public static bool IsRegistered
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, false);
            return key?.GetValue(AppName) != null;
        }
    }
    
    /// <summary>スタートアップ登録をトグル</summary>
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
    
    /// <summary>スタートアップに登録</summary>
    public static void Register()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
        if (key != null)
        {
            string exePath = Environment.ProcessPath ?? Application.ExecutablePath;
            key.SetValue(AppName, $"\"{exePath}\"");
        }
    }
    
    /// <summary>スタートアップから削除</summary>
    public static void Unregister()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
        key?.DeleteValue(AppName, false);
    }
}
