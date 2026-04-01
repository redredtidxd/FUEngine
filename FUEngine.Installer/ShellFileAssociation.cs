using Microsoft.Win32;

namespace FUEngine.Installer;

/// <summary>Asocia .FUE y .fueproj con FUEngine.exe (HKCU).</summary>
internal static class ShellFileAssociation
{
    private static readonly string[] Extensions = { ".FUE", ".fueproj" };

    internal static void TryRegister(string installDir)
    {
        var exe = Path.Combine(installDir, "FUEngine.exe");
        if (!File.Exists(exe)) return;

        var progId = InstallConstants.FileAssociationProgId;
        try
        {
            using (var root = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}", writable: true))
            {
                root?.SetValue("", "Proyecto FUEngine");
                using (var icon = root?.CreateSubKey("DefaultIcon"))
                    icon?.SetValue("", $"\"{exe}\",0");
                using (var cmd = root?.CreateSubKey(@"shell\open\command"))
                    cmd?.SetValue("", $"\"{exe}\" \"%1\"");
            }

            foreach (var ext in Extensions)
            {
                using var extKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ext}", writable: true);
                extKey?.SetValue("", progId);
            }
        }
        catch
        {
            /* ignore */
        }
    }

    internal static void TryUnregister()
    {
        try
        {
            using var classes = Registry.CurrentUser.OpenSubKey(@"Software\Classes", true);
            if (classes == null) return;

            try
            {
                classes.DeleteSubKeyTree(InstallConstants.FileAssociationProgId, false);
            }
            catch
            {
                /* ignore */
            }

            foreach (var ext in Extensions)
            {
                try
                {
                    using var extKey = classes.OpenSubKey(ext, false);
                    var defaultVal = extKey?.GetValue("") as string;
                    if (string.Equals(defaultVal, InstallConstants.FileAssociationProgId, StringComparison.OrdinalIgnoreCase))
                        classes.DeleteSubKey(ext, false);
                }
                catch
                {
                    /* ignore */
                }
            }
        }
        catch
        {
            /* ignore */
        }
    }
}
