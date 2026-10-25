namespace yhwndy;

static class Program
{
    private static Mutex? _mutex;
    
    [STAThread]
    static void Main()
    {
        const string mutexName = "yhwndy_singleton_mutex";
        _mutex = new Mutex(true, mutexName, out bool createdNew);
        
        if (!createdNew)
        {
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