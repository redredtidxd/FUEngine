using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace FUEngine.Installer;

internal static class InstallOperations
{
    private static IEnumerable<Assembly> CandidateAssemblies()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var asm in new Assembly?[]
                 {
                     typeof(InstallOperations).Assembly,
                     Assembly.GetEntryAssembly(),
                     Assembly.GetExecutingAssembly(),
                 })
        {
            if (asm == null) continue;
            var key = asm.FullName ?? asm.GetName().Name ?? "";
            if (seen.Add(key))
                yield return asm;
        }

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (asm.IsDynamic) continue;
            var key = asm.FullName ?? asm.GetName().Name ?? "";
            if (!seen.Add(key)) continue;
            yield return asm;
        }
    }

    /// <summary>Recurso embebido del motor (nombre interno MotorPack; formato ZIP).</summary>
    private static bool MatchesMotorPackResourceName(string name)
    {
        if (name.EndsWith("MotorPack", StringComparison.OrdinalIgnoreCase))
            return true;
        if (name.IndexOf(".MotorPack", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        return false;
    }

    internal static bool HasEmbeddedPayload()
    {
        foreach (var asm in CandidateAssemblies())
        {
            try
            {
                foreach (var n in asm.GetManifestResourceNames())
                {
                    if (MatchesMotorPackResourceName(n))
                        return true;
                }
            }
            catch
            {
                /* ensamblado dinámico u otro */
            }
        }

        return false;
    }

    internal static bool HasInstallablePayload() => HasEmbeddedPayload();

    /// <summary>
    /// Borra solo la carpeta de instalación del motor (p. ej. Archivos de programa\FUEngine).
    /// No afecta a los proyectos del usuario, que viven en otras rutas y en %LocalAppData%\FUEngine.
    /// </summary>
    internal static void PrepareInstallDirectory(string installDir, IProgress<string>? progress = null)
    {
        if (!Directory.Exists(installDir))
        {
            Directory.CreateDirectory(installDir);
            return;
        }

        var marker = Path.Combine(installDir, "FUEngine.exe");
        if (File.Exists(marker))
            progress?.Report("Reemplazando instalación anterior…");

        try
        {
            DeleteDirectoryRecursiveRobust(installDir);
        }
        catch (IOException ex)
        {
            throw new IOException(
                "No se pudo sustituir los archivos (¿FUEngine u otro programa tiene abierta la carpeta?). Cierra el editor y vuelve a intentar.",
                ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new IOException(
                "No se pudo sustituir los archivos (permiso denegado o carpeta en uso). Cierra FUEngine y vuelve a intentar.",
                ex);
        }

        Directory.CreateDirectory(installDir);
    }

    private static void DeleteDirectoryRecursiveRobust(string path)
    {
        if (!Directory.Exists(path)) return;
        foreach (var sub in Directory.EnumerateDirectories(path))
            DeleteDirectoryRecursiveRobust(sub);
        foreach (var file in Directory.EnumerateFiles(path))
        {
            TryClearReadOnly(file);
            File.Delete(file);
        }

        TryClearReadOnly(path);
        Directory.Delete(path, false);
    }

    private static void TryClearReadOnly(string path)
    {
        try
        {
            var a = File.GetAttributes(path);
            if ((a & FileAttributes.ReadOnly) != 0)
                File.SetAttributes(path, a & ~FileAttributes.ReadOnly);
        }
        catch
        {
            /* best effort */
        }
    }

    internal static void InstallEngineTo(string installDir, IProgress<string>? progress)
    {
        using var stream = OpenEmbeddedMotorPackStream();
        InstallFromZipStream(stream, installDir, progress);
    }

    private static Stream OpenEmbeddedMotorPackStream()
    {
        foreach (var exact in new[]
                 {
                     "MotorPack",
                     "FUEngine.Installer.MotorPack",
                     "InstalarFUEngine.MotorPack",
                 })
        {
            foreach (var asm in CandidateAssemblies())
            {
                try
                {
                    var s = asm.GetManifestResourceStream(exact);
                    if (s != null) return s;
                }
                catch
                {
                    /* siguiente */
                }
            }
        }

        foreach (var asm in CandidateAssemblies())
        {
            try
            {
                foreach (var n in asm.GetManifestResourceNames())
                {
                    if (!MatchesMotorPackResourceName(n)) continue;
                    var s = asm.GetManifestResourceStream(n);
                    if (s != null) return s;
                }
            }
            catch
            {
                /* siguiente */
            }
        }

        throw new InvalidOperationException(
            "Este instalador no contiene el motor. Genera de nuevo el paquete con installer\\build-installer.ps1 (Release).");
    }

    private static void InstallFromZipStream(Stream stream, string installDir, IProgress<string>? progress)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "FUEngineInstall_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            var zipPath = Path.Combine(tempRoot, "payload.zip");
            progress?.Report("Instalando…");
            using (var fs = File.Create(zipPath))
                stream.CopyTo(fs);

            var extractDir = Path.Combine(tempRoot, "extract");
            Directory.CreateDirectory(extractDir);
            ZipFile.ExtractToDirectory(zipPath, extractDir);

            CopyDirectory(extractDir, installDir);
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempRoot))
                    Directory.Delete(tempRoot, recursive: true);
            }
            catch
            {
                /* ignore */
            }
        }

        RegisterUninstall(installDir);
        StartMenuShortcuts.TryCreate(installDir);
        ShellFileAssociation.TryRegister(installDir);
    }

    internal static void TryCreateDesktopShortcut(string installDir)
    {
        var exe = Path.Combine(installDir, "FUEngine.exe");
        if (File.Exists(exe))
            DesktopShortcut.TryCreate(exe, InstallConstants.ProductName, installDir);
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        foreach (var dir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(sourceDir, dir);
            Directory.CreateDirectory(Path.Combine(destDir, rel));
        }

        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(sourceDir, file);
            var dest = Path.Combine(destDir, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(file, dest, overwrite: true);
        }
    }

    private static void RegisterUninstall(string installDir)
    {
        var exePath = Path.Combine(installDir, "FUEngine.exe");
        var uninstallLine = $"\"{Application.ExecutablePath}\" /uninstall";

        using var key = Registry.CurrentUser.CreateSubKey(InstallConstants.RegistryKeyUninstall, writable: true)
                        ?? throw new InvalidOperationException("No se pudo crear la clave de desinstalación.");

        key.SetValue("DisplayName", InstallConstants.ProductName);
        key.SetValue("InstallLocation", installDir);
        key.SetValue("DisplayIcon", exePath, RegistryValueKind.String);
        key.SetValue("UninstallString", uninstallLine);
        key.SetValue("Publisher", InstallConstants.PublisherName);
        try
        {
            key.SetValue("DisplayVersion", Application.ProductVersion ?? "1.0.0");
        }
        catch
        {
            /* optional */
        }
    }

    internal static string GetDefaultInstallPath()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (string.IsNullOrEmpty(programFiles))
            programFiles = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs");
        return Path.Combine(programFiles, InstallConstants.ProductName);
    }
}
