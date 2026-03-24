using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using FUEngine.Core;
using FUEngine.Editor;

namespace FUEngine;

public partial class App : System.Windows.Application
{
    private Mutex? _instanceMutex;

    /// <summary>Evita miles de líneas idénticas en session_*.log cuando un fallo de layout se repite (p. ej. VirtualizingStackPanel).</summary>
    private static readonly object _uiCrashLock = new();
    private static string? _lastUiCrashSignature;
    private static int _suppressedRepeatUiCrashes;

    private void App_OnStartup(object sender, StartupEventArgs e)
    {
        RegisterGlobalExceptionHandlers();

        if (PlayerLaunchArgs.TryParse(e.Args, out var dataDir))
        {
            if (!EnsureSinglePlayerInstance()) return;
            try
            {
                var player = new PlayerWindow(dataDir);
                player.Show();
                Current.MainWindow = player;
            }
            catch (Exception ex)
            {
                EditorLog.Error($"Player: {ex}");
                System.Windows.MessageBox.Show(
                    $"Error al iniciar el juego:\n{ex.Message}",
                    "FUEngine",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
            }
            return;
        }

        if (!EnsureSingleInstance()) return;

        try
        {
            var splash = new SplashScreenWindow(SplashScreenConfig.Default);
            splash.Show();

            splash.RunThenClose(() =>
            {
                var startup = new StartupWindow();
                startup.Show();
                Current.MainWindow = startup;
            });
        }
        catch (Exception ex)
        {
            EditorLog.Error($"Error fatal en startup: {ex}");
            System.Windows.MessageBox.Show(
                $"Error al iniciar FUEngine:\n{ex.Message}",
                "Error Fatal",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private bool EnsureSingleInstance()
    {
        const string name = "FUEngine_SingleInstance";

        try
        {
            _instanceMutex = new Mutex(true, name, out bool createdNew);
            if (createdNew) return true;

            System.Windows.MessageBox.Show("FUEngine ya está en ejecución.", "Aviso",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown(0);
            return false;
        }
        catch (Exception ex)
        {
            EditorLog.Error($"Error verificando instancia única: {ex.Message}");
            return true; // Modo degradado: permitir arranque
        }
    }

    private bool EnsureSinglePlayerInstance()
    {
        const string name = "FUEngine_Player_SingleInstance";
        try
        {
            _instanceMutex = new Mutex(true, name, out bool createdNew);
            if (createdNew) return true;
            System.Windows.MessageBox.Show("Ya hay una instancia del juego en ejecución.", "FUEngine",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown(0);
            return false;
        }
        catch (Exception ex)
        {
            EditorLog.Error($"Player instancia única: {ex.Message}");
            return true;
        }
    }

    private bool TryOpenRecentProject()
    {
        var recent = StartupService.LoadMostRecent();
        if (recent == null) return false;

        if (!File.Exists(recent.Path))
        {
            EditorLog.Warning($"Proyecto reciente no encontrado: {recent.Path}");
            StartupService.RemoveFromRecent(recent.Path);
            return false;
        }

        try
        {
            if (!ProjectFormatOpenHelper.TryPromptAndLoad(recent.Path, Current.MainWindow, out var project, out var loadErr))
            {
                if (!string.IsNullOrEmpty(loadErr))
                    EditorLog.Warning($"Error cargando proyecto reciente '{recent.Path}': {loadErr}");
                return false;
            }
            if (project == null) return false;

            // Refrescar en lista de recientes (actualiza orden/timestamp). Fallback a recent si project no tiene nombre/descripción.
            StartupService.AddRecentProject(
                recent.Path,
                project.Nombre ?? recent.Name ?? "Sin nombre",
                project.Descripcion ?? recent.Description ?? "",
                EngineVersion.Current);

            OpenEditor(project);
            return true;
        }
        catch (JsonException jex)
        {
            EditorLog.Critical($"JSON corrupto en proyecto reciente ({recent.Path}): línea {jex.LineNumber}, posición {jex.BytePositionInLine}: {jex.Message}", "IO");
            return false;
        }
        catch (Exception ex)
        {
            EditorLog.Warning($"Error cargando proyecto reciente '{recent.Path}' ({ex.GetType().Name}): {ex.Message}");
            return false;
        }
    }

    private void OpenEditor(ProjectInfo project)
    {
        var editor = new EditorWindow(project);
        editor.Show();
        Current.MainWindow = editor;
    }

    /// <summary>Evita reentrada: <see cref="EditorLog"/> actualiza la consola durante el mismo ciclo que falló el layout.</summary>
    private static void QueueDeferredUiCrashLog(string message)
    {
        if (Current?.Dispatcher == null)
        {
            try { EditorLog.Critical(message, "ENGINE"); } catch { /* ignore */ }
            return;
        }
        Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
            new Action(() =>
            {
                try { EditorLog.Critical(message, "ENGINE"); } catch { /* ignore */ }
            }));
    }

    /// <summary>Registra manejadores para que todas las excepciones no capturadas se envíen a la consola del editor.</summary>
    private void RegisterGlobalExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            var msg = ex != null ? $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}" : args.ExceptionObject?.ToString() ?? "Error desconocido";
            EditorLog.Critical($"CRASH MOTOR (AppDomain): {msg}", "FATAL");
        };

        DispatcherUnhandledException += (_, args) =>
        {
            args.Handled = true;
            var ex = args.Exception;
            // Firmar sin stack: miles de frames pueden lanzar el mismo error (p. ej. plantilla XAML + VirtualizingStackPanel).
            var sig = $"{ex.GetType().FullName}\0{ex.Message}";
            lock (_uiCrashLock)
            {
                if (string.Equals(_lastUiCrashSignature, sig, StringComparison.Ordinal))
                {
                    _suppressedRepeatUiCrashes++;
                    return;
                }

                if (_suppressedRepeatUiCrashes > 0 && !string.IsNullOrEmpty(_lastUiCrashSignature))
                {
                    var n = _suppressedRepeatUiCrashes;
                    _suppressedRepeatUiCrashes = 0;
                    QueueDeferredUiCrashLog(
                        $"CRASH UI (resumen): se suprimieron {n} repeticiones inmediatas de la misma excepción (evita reentrada en el log).");
                }

                _lastUiCrashSignature = sig;
            }

            QueueDeferredUiCrashLog(
                $"CRASH UI: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            if (args.Exception?.InnerExceptions != null)
            {
                foreach (var ex in args.Exception.InnerExceptions)
                    EditorLog.Error($"Tarea no observada: {ex.GetType().Name}: {ex.Message}", "Async");
            }
            args.SetObserved();
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        lock (_uiCrashLock)
        {
            if (_suppressedRepeatUiCrashes > 0)
            {
                var n = _suppressedRepeatUiCrashes;
                _suppressedRepeatUiCrashes = 0;
                try
                {
                    EditorLog.Critical(
                        $"CRASH UI (resumen al salir): {n} repeticiones más de la misma excepción no se registraron por separado (anti-spam).",
                        "ENGINE");
                }
                catch { /* ignore */ }
            }
        }
        _instanceMutex?.Dispose();
        DiscordRichPresenceService.Instance.Shutdown();
        base.OnExit(e);
    }
}
