using System;
using System.IO;
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
            var project = ProjectSerialization.Load(recent.Path);

            // Refrescar en lista de recientes (actualiza orden/timestamp). Fallback a recent si project no tiene nombre/descripción.
            StartupService.AddRecentProject(
                recent.Path,
                project.Nombre ?? recent.Name ?? "Sin nombre",
                project.Descripcion ?? recent.Description ?? "",
                EngineVersion.Current);

            OpenEditor(project);
            return true;
        }
        catch (Exception ex)
        {
            EditorLog.Warning($"Error cargando proyecto reciente '{recent.Path}' ({ex.GetType().Name}): {ex.Message}");
            if (ex is System.Text.Json.JsonException)
            {
                EditorLog.Info("El archivo del proyecto puede estar corrupto o ser de una versión incompatible.");
            }
            return false;
        }
    }

    private void OpenEditor(ProjectInfo project)
    {
        var editor = new EditorWindow(project);
        editor.Show();
        Current.MainWindow = editor;
    }

    /// <summary>Registra manejadores para que todas las excepciones no capturadas se envíen a la consola del editor.</summary>
    private void RegisterGlobalExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            var msg = ex != null ? $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}" : args.ExceptionObject?.ToString() ?? "Error desconocido";
            EditorLog.Error($"Excepción no controlada (AppDomain): {msg}", "Sistema");
        };

        DispatcherUnhandledException += (_, args) =>
        {
            var ex = args.Exception;
            EditorLog.Error($"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}", "UI");
            args.Handled = true;
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
        _instanceMutex?.Dispose();
        base.OnExit(e);
    }
}
