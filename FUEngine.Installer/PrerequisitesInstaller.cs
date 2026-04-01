using System.Diagnostics;
using System.Net.Http;
using Microsoft.Win32;

namespace FUEngine.Installer;

/// <summary>
/// Visual C++ x64, DirectX End-User Runtime (web) y comprobación opcional de .NET 8 Desktop.
/// El motor publicado es autocontenido: .NET Desktop no es obligatorio para ejecutar FUEngine.exe.
/// </summary>
internal static class PrerequisitesInstaller
{
    private const string VcRedistUrl = "https://aka.ms/vs/17/release/vc_redist.x64.exe";
    /// <summary>Instalador web de DirectX End-User Runtime (Microsoft Download Center, id=35).</summary>
    private const string DxWebSetupUrl =
        "https://download.microsoft.com/download/1/7/1/1718ccc4-6315-4d8e-9543-8e28a4e18c4c/dxwebsetup.exe";
    private const string DxDownloadPageUrl = "https://www.microsoft.com/download/details.aspx?id=35";
    private const string DotNet8DesktopLandingUrl = "https://dotnet.microsoft.com/download/dotnet/8.0";

    internal static void Run(PrerequisiteOptions options, IProgress<string>? progress)
    {
        if (options.InstallVcRedistX64)
            EnsureVcRedistX64(progress);

        if (options.DirectXEndUserRuntime)
            EnsureDirectXEndUserRuntime(progress);

        if (options.DotNet8DesktopDownloadIfMissing)
            OfferDotNet8DesktopIfMissing(progress);
    }

    private static void EnsureVcRedistX64(IProgress<string>? progress)
    {
        if (IsVcRedist140X64Installed())
            return;

        progress?.Report("Instalando Visual C++ 2015-2022 (x64)…");
        var temp = Path.Combine(Path.GetTempPath(), "FUEngine_vc_redist.x64.exe");
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            var bytes = http.GetByteArrayAsync(VcRedistUrl).GetAwaiter().GetResult();
            File.WriteAllBytes(temp, bytes);

            using var p = Process.Start(new ProcessStartInfo
            {
                FileName = temp,
                Arguments = "/install /quiet /norestart",
                UseShellExecute = true,
            });
            p?.WaitForExit(600_000);
        }
        catch
        {
            /* continuar */
        }
        finally
        {
            try
            {
                if (File.Exists(temp))
                    File.Delete(temp);
            }
            catch
            {
                /* ignore */
            }
        }
    }

    private static void EnsureDirectXEndUserRuntime(IProgress<string>? progress)
    {
        progress?.Report("DirectX End-User Runtime (web)…");
        var temp = Path.Combine(Path.GetTempPath(), "FUEngine_dxwebsetup.exe");
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            var bytes = http.GetByteArrayAsync(DxWebSetupUrl).GetAwaiter().GetResult();
            File.WriteAllBytes(temp, bytes);

            using var p = Process.Start(new ProcessStartInfo
            {
                FileName = temp,
                Arguments = "/Q",
                UseShellExecute = true,
            });
            p?.WaitForExit(600_000);
        }
        catch
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = DxDownloadPageUrl,
                    UseShellExecute = true,
                });
            }
            catch
            {
                /* ignore */
            }
        }
        finally
        {
            try
            {
                if (File.Exists(temp))
                    File.Delete(temp);
            }
            catch
            {
                /* ignore */
            }
        }
    }

    private static void OfferDotNet8DesktopIfMissing(IProgress<string>? progress)
    {
        if (IsDotNet8WindowsDesktopRuntimeInstalled())
            return;

        progress?.Report("Abriendo descarga de .NET 8 Desktop Runtime…");
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = DotNet8DesktopLandingUrl,
                UseShellExecute = true,
            });
        }
        catch
        {
            /* ignore */
        }
    }

    private static bool IsVcRedist140X64Installed()
    {
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
            using var k = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64");
            var v = k?.GetValue("Installed");
            if (v is int i && i == 1) return true;
            if (v is long l && l == 1) return true;
        }

        return false;
    }

    /// <summary>Comprueba si existe un runtime Microsoft.WindowsDesktop.App 8.x (Program Files\dotnet\shared).</summary>
    private static bool IsDotNet8WindowsDesktopRuntimeInstalled()
    {
        try
        {
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var baseDir = Path.Combine(programFiles, "dotnet", "shared", "Microsoft.WindowsDesktop.App");
            if (!Directory.Exists(baseDir)) return false;
            foreach (var d in Directory.EnumerateDirectories(baseDir))
            {
                var name = Path.GetFileName(d);
                if (name.StartsWith("8.", StringComparison.Ordinal))
                    return true;
            }
        }
        catch
        {
            /* ignore */
        }

        return false;
    }
}
