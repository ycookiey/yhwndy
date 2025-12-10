namespace yhwndy;

static class Program
{
    private static Mutex? _mutex;
    
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // 複数起動禁止
        const string mutexName = "yhwndy_singleton_mutex";
        _mutex = new Mutex(true, mutexName, out bool createdNew);
        
        if (!createdNew)
        {
            // 既に起動中 → 静かに終了
            return;
        }
        
        try
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
        finally
        {
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
        }
    }
}