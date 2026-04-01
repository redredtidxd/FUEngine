using System.Windows.Forms;

namespace FUEngine.Installer;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        if (args.Length > 0 && args[0].Equals("/uninstall", StringComparison.OrdinalIgnoreCase))
        {
            UninstallRunner.RunInteractive();
            return;
        }

        Application.Run(new InstallForm());
    }
}
