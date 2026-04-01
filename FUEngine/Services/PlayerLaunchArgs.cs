using System.IO;
using System.Reflection;

namespace FUEngine;

/// <summary>Argumentos de línea de comandos para arrancar solo el reproductor (sin editor).</summary>
public static class PlayerLaunchArgs
{
    /// <summary>
    /// true si se debe abrir <see cref="PlayerWindow"/> con <see cref="DataDirectory"/>.
    /// Formas: <c>--play "C:\ruta\Data"</c>, <c>--player "C:\ruta\Data"</c>, o sin args si el exe no es FUEngine.exe y existe <c>Data/</c> junto al exe (build exportada).
    /// </summary>
    public static bool TryParse(string[]? args, out string dataDirectory)
    {
        dataDirectory = "";
        if (args == null || args.Length == 0)
        {
            var exeName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
            if (string.Equals(exeName, "FUEngine.exe", StringComparison.OrdinalIgnoreCase))
                return false;
            var def = Path.Combine(AppContext.BaseDirectory, "Data");
            if (Directory.Exists(def))
            {
                dataDirectory = Path.GetFullPath(def);
                return true;
            }
            return false;
        }

        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (!string.Equals(a, "--play", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(a, "--player", StringComparison.OrdinalIgnoreCase))
                continue;
            if (i + 1 >= args.Length) return false;
            var path = args[i + 1].Trim().Trim('"');
            if (string.IsNullOrEmpty(path)) return false;
            if (!Path.IsPathRooted(path))
                path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
            else
                path = Path.GetFullPath(path);
            dataDirectory = path;
            return Directory.Exists(dataDirectory);
        }

        return false;
    }
}
