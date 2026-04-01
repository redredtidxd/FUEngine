namespace FUEngine.Installer;

internal static class StartMenuShortcuts
{
    internal static void TryCreate(string installDir)
    {
        var exe = Path.Combine(installDir, "FUEngine.exe");
        if (!File.Exists(exe)) return;

        var programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
        var folder = Path.Combine(programs, InstallConstants.StartMenuRelativeFolder);
        var winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var explorer = Path.Combine(winDir, "explorer.exe");
        var logs = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FUEngine", "logs");

        ShortcutHelper.TryCreate(Path.Combine(folder, "FUEngine.lnk"), exe, "", installDir, exe);
        ShortcutHelper.TryCreate(Path.Combine(folder, "Carpeta de logs (soporte).lnk"), explorer, $"\"{logs}\"", installDir, null);
        ShortcutHelper.TryCreate(Path.Combine(folder, "Manual y ayuda (en el motor).lnk"), exe, "", installDir, exe);
    }

    internal static void TryRemove()
    {
        try
        {
            var programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            var folder = Path.Combine(programs, InstallConstants.StartMenuRelativeFolder);
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
            var parent = Path.Combine(programs, "Red Redtid");
            if (Directory.Exists(parent) && Directory.GetFileSystemEntries(parent).Length == 0)
                Directory.Delete(parent);
        }
        catch
        {
            /* ignore */
        }
    }
}
