using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using FUEngine.Core;
using FUEngine.Editor;

namespace FUEngine;

/// <summary>
/// Empaqueta el ejecutable actual (carpeta del motor) + copia del proyecto en <c>Data/</c> para distribuci?n.
/// </summary>
public sealed class ProjectBuildService
{
    private static readonly HashSet<string> ExcludeTopLevelNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Data"
    };

    /// <summary>
    /// <paramref name="sceneIndex"/> &lt; 0: mantener MainMapPath / MainObjectsPath del proyecto.
    /// </summary>
    public static void Build(
        ProjectInfo project,
        string projectRootDirectory,
        string outputDirectory,
        string executableBaseName,
        int sceneIndex,
        bool useDotnetPublish,
        Action<string>? log)
    {
        log ??= _ => { };
        if (string.IsNullOrWhiteSpace(projectRootDirectory) || !Directory.Exists(projectRootDirectory))
            throw new ArgumentException("Carpeta del proyecto no v?lida.", nameof(projectRootDirectory));
        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("Carpeta de salida no v?lida.", nameof(outputDirectory));
        executableBaseName = SanitizeExeName(executableBaseName);
        if (string.IsNullOrEmpty(executableBaseName))
            executableBaseName = "Game";

        var engineDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (string.IsNullOrEmpty(engineDir) || !Directory.Exists(engineDir))
            throw new InvalidOperationException("No se pudo localizar la carpeta del ejecutable del motor.");

        Directory.CreateDirectory(outputDirectory);
        log($"Salida: {outputDirectory}");

        if (useDotnetPublish)
        {
            if (!TryPublishSelfContainedTo(outputDirectory, log))
                throw new InvalidOperationException("dotnet publish no se complet? correctamente.");
        }
        else
        {
            log($"Motor (copia): {engineDir}");
            CopyEngineDirectory(engineDir, outputDirectory, log);
        }

        TryRenameEngineExecutable(outputDirectory, executableBaseName, log);
        WriteDataBundle(project, projectRootDirectory, outputDirectory, sceneIndex, log);
        WriteReadme(outputDirectory, executableBaseName, log);
        log("Build completado.");
    }

    private static void TryRenameEngineExecutable(string outputDirectory, string executableBaseName, Action<string> log)
    {
        if (string.Equals(executableBaseName, "FUEngine", StringComparison.OrdinalIgnoreCase))
            return;
        var srcExe = Path.Combine(outputDirectory, "FUEngine.exe");
        var dstExe = Path.Combine(outputDirectory, executableBaseName + ".exe");
        if (!File.Exists(srcExe))
        {
            log("Aviso: no se encontr? FUEngine.exe para renombrar.");
            return;
        }

        if (string.Equals(Path.GetFullPath(srcExe), Path.GetFullPath(dstExe), StringComparison.OrdinalIgnoreCase))
            return;
        if (File.Exists(dstExe)) File.Delete(dstExe);
        File.Move(srcExe, dstExe);
    }

    private static void WriteDataBundle(
        ProjectInfo project,
        string projectRootDirectory,
        string outputDirectory,
        int sceneIndex,
        Action<string> log)
    {
        var dataDir = Path.Combine(outputDirectory, "Data");
        if (Directory.Exists(dataDir))
            Directory.Delete(dataDir, recursive: true);
        Directory.CreateDirectory(dataDir);

        CopyProjectForData(projectRootDirectory, dataDir, log);

        var exportJson = Path.Combine(dataDir, "proyecto.json");
        ProjectExportHelper.WriteExportProjectJson(project, exportJson, sceneIndex);
        log($"Proyecto export: {exportJson}");
        WriteAdsExportMetadata(project, dataDir, log);
    }

    private static void WriteAdsExportMetadata(ProjectInfo project, string dataDir, Action<string> log)
    {
        try
        {
            var provider = string.IsNullOrWhiteSpace(project.AdsExportProvider)
                ? "simulated"
                : project.AdsExportProvider!.Trim();
            var path = Path.Combine(dataDir, "ads_export.json");
            var dto = new
            {
                provider,
                editorRuntime = "SimulatedAdsApi",
                nativeNote = string.Equals(provider, "google_mobile_ads", StringComparison.OrdinalIgnoreCase)
                    ? "Host Android/iOS: registrar GoogleMobileAdsApi (SDK) en lugar del simulador; ver FUEngine.Runtime/GoogleMobileAdsApi.cs y LICENSE.md."
                    : (string?)null,
                generatedUtc = DateTime.UtcNow
            };
            File.WriteAllText(path, JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true }));
            log($"Ads export: {path} (provider={provider})");
        }
        catch (Exception ex)
        {
            log($"Aviso: no se pudo escribir ads_export.json: {ex.Message}");
        }
    }

    private static void WriteReadme(string outputDirectory, string exeName, Action<string> log)
    {
        var readme = Path.Combine(outputDirectory, "LEEME.txt");
        try
        {
            File.WriteAllText(readme,
                $"FUEngine ÿÿÿ build exportada\r\n\r\n" +
                $"Ejecuta {exeName}.exe para iniciar el juego.\r\n" +
                $"La carpeta \"Data\" debe permanecer junto al ejecutable (contiene el proyecto y assets).\r\n" +
                $"Requisito: .NET 8 Runtime (Windows) si el build no es self-contained.\r\n");
        }
        catch (Exception ex)
        {
            log($"Aviso: no se pudo escribir LEEME.txt: {ex.Message}");
        }
    }

    /// <summary>Nombre de archivo exe sin extensi?n (caracteres inv?lidos eliminados).</summary>
    public static string SanitizeExecutableBaseName(string? raw) => SanitizeExeName(raw ?? "");

    private static string SanitizeExeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";
        var chars = Path.GetInvalidFileNameChars();
        var s = new string(name.Where(c => !chars.Contains(c)).ToArray()).Trim();
        if (s.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            s = s[..^4];
        return s;
    }

    private static void CopyEngineDirectory(string sourceDir, string destDir, Action<string> log)
    {
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fn = Path.GetFileName(file);
            if (fn.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase)) continue;
            File.Copy(file, Path.Combine(destDir, fn), true);
        }

        foreach (var sub in Directory.GetDirectories(sourceDir))
        {
            var name = Path.GetFileName(sub);
            if (ExcludeTopLevelNames.Contains(name)) continue;
            var targetSub = Path.Combine(destDir, name);
            CopyDirectoryRecursive(sub, targetSub, log);
        }
    }

    private static void CopyProjectForData(string projectRoot, string dataDir, Action<string> log)
    {
        foreach (var entry in Directory.GetFileSystemEntries(projectRoot))
        {
            var name = Path.GetFileName(entry);
            if (ShouldExcludeProjectEntry(name)) continue;

            var dest = Path.Combine(dataDir, name);
            if (Directory.Exists(entry))
                CopyDirectoryRecursive(entry, dest, log);
            else
                File.Copy(entry, dest, true);
        }
    }

    private static bool ShouldExcludeProjectEntry(string name) =>
        name.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
        name.Equals(".vs", StringComparison.OrdinalIgnoreCase) ||
        name.Equals(".git", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Autoguardados", StringComparison.OrdinalIgnoreCase);

    private static void CopyDirectoryRecursive(string sourceDir, string destDir, Action<string> log)
    {
        Directory.CreateDirectory(destDir);
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fn = Path.GetFileName(file);
            if (fn.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase)) continue;
            File.Copy(file, Path.Combine(destDir, fn), true);
        }

        foreach (var sub in Directory.GetDirectories(sourceDir))
        {
            var name = Path.GetFileName(sub);
            if (ShouldExcludeProjectEntry(name)) continue;
            CopyDirectoryRecursive(sub, Path.Combine(destDir, name), log);
        }
    }

    /// <summary>Intenta publicar con <c>dotnet publish</c> (opcional, si existe el .csproj en el ?rbol superior al exe).</summary>
    public static bool TryPublishSelfContainedTo(string outputDir, Action<string> log)
    {
        log ??= _ => { };
        var csproj = FindFUEngineCsproj();
        if (string.IsNullOrEmpty(csproj))
        {
            log("No se encontr? FUEngine.csproj (publish omitido; se us? la carpeta del exe).");
            return false;
        }

        try
        {
            Directory.CreateDirectory(outputDir);
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                ArgumentList =
                {
                    "publish", csproj,
                    "-c", "Release",
                    "-r", "win-x64",
                    "--self-contained", "true",
                    "-o", outputDir
                },
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p == null) return false;
            var err = p.StandardError.ReadToEnd();
            var stdout = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            if (p.ExitCode != 0)
            {
                log($"dotnet publish fall? ({p.ExitCode}): {err}");
                return false;
            }
            if (!string.IsNullOrWhiteSpace(stdout)) log(stdout.Trim());
            return true;
        }
        catch (Exception ex)
        {
            log($"dotnet publish: {ex.Message}");
            return false;
        }
    }

    private static string? FindFUEngineCsproj()
    {
        var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        for (int i = 0; i < 12 && !string.IsNullOrEmpty(dir); i++)
        {
            var p = Path.Combine(dir, "FUEngine.csproj");
            if (File.Exists(p)) return p;
            dir = Directory.GetParent(dir)?.FullName;
        }
        return null;
    }
}
