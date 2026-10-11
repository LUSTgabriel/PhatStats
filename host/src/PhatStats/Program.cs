using PhatStats.App;

namespace PhatStats;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        using var instance = SingleInstanceGate.Open();
        if (!instance.IsPrimary)
        {
            instance.SignalExisting();
            return;
        }

        using var context = new PhatStatsApplicationContext(instance);
        Application.Run(context);
    }
}
