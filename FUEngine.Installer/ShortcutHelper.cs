namespace FUEngine.Installer;

internal static class ShortcutHelper
{
    internal static void TryCreate(string lnkPath, string targetPath, string arguments, string workingDirectory, string? iconPath)
    {
        try
        {
            var dir = Path.GetDirectoryName(lnkPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return;

            dynamic wsh = Activator.CreateInstance(shellType)!;
            dynamic sc = wsh.CreateShortcut(lnkPath);
            sc.TargetPath = targetPath;
            sc.Arguments = arguments;
            sc.WorkingDirectory = workingDirectory;
            if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
                sc.IconLocation = iconPath + ",0";
            else if (File.Exists(targetPath))
                sc.IconLocation = targetPath + ",0";
            sc.Save();
        }
        catch
        {
            /* best effort */
        }
    }
}
