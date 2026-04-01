using System.Windows.Forms;
using Microsoft.Win32;

namespace FUEngine.Installer;

internal static class UninstallRunner
{
    internal static void RunInteractive()
    {
        using var key = Registry.CurrentUser.OpenSubKey(InstallConstants.RegistryKeyUninstall);
        var loc = key?.GetValue("InstallLocation") as string;
        if (string.IsNullOrEmpty(loc) || !Directory.Exists(loc))
        {
            MessageBox.Show(
                "No se encontró una instalación registrada en este usuario.",
                InstallConstants.ProductName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        if (MessageBox.Show(
                $"¿Desinstalar FUEngine de esta carpeta?\n\n{loc}",
                "Confirmar desinstalación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        try
        {
            ShellFileAssociation.TryUnregister();
            StartMenuShortcuts.TryRemove();
            Directory.Delete(loc, recursive: true);
            Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall", true)
                ?.DeleteSubKey("FUEngine", throwOnMissingSubKey: false);
            MessageBox.Show("Desinstalación completada.", InstallConstants.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error: " + ex.Message, InstallConstants.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
