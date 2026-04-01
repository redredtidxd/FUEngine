namespace FUEngine.Installer;

/// <summary>Crea un .lnk en el escritorio (icono del ejecutable).</summary>
internal static class DesktopShortcut
{
    internal static void TryCreate(string targetExePath, string shortcutFileNameWithoutExtension, string? workingDirectory = null)
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var lnkPath = Path.Combine(desktop, shortcutFileNameWithoutExtension + ".lnk");
        ShortcutHelper.TryCreate(lnkPath, targetExePath, "", workingDirectory ?? Path.GetDirectoryName(targetExePath) ?? "", targetExePath);
    }
}
