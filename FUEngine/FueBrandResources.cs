using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace FUEngine;

/// <summary>Logo oficial del motor (PNG en output/publish y recurso para iconos de ventana).</summary>
internal static class FueBrandResources
{
    internal const string LogoFileName = "mando_logo_de_fuengine.png";

    internal static string? TryGetEngineLogoPath()
    {
        var p = Path.Combine(AppContext.BaseDirectory, "Resources", LogoFileName);
        return File.Exists(p) ? p : null;
    }

    internal static void ApplyToApplication(System.Windows.Application app)
    {
        var path = TryGetEngineLogoPath();
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(path, UriKind.Absolute);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            app.Resources["AppWindowIcon"] = bmp;
            app.Resources["AppBrandLogoImage"] = bmp;
        }
        catch
        {
            /* sin logo en disco o imagen inválida */
        }
    }
}
