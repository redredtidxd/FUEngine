using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using FUEngine.Core;
using FUEngine.Editor;
using FUEngine.Input;
using FUEngine.Rendering;
using FUEngine.Runtime;

namespace FUEngine;

/// <summary>Ventana mínima: solo viewport de juego, sin editor.</summary>
public partial class PlayerWindow : Window
{
    private readonly string _dataDirectory;
    private PlayModeRunner? _runner;
    private TextureAssetCache? _textureCache;
    private DispatcherTimer? _renderTimer;
    private ProjectInfo? _project;
    private readonly PlayKeyboardSnapshot _playKeyboard = new();

    public PlayerWindow(string dataDirectory)
    {
        _dataDirectory = dataDirectory ?? throw new ArgumentNullException(nameof(dataDirectory));
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var projectFile = FindProjectFile(_dataDirectory);
            if (string.IsNullOrEmpty(projectFile))
            {
                ShowFatal("No se encontró Project.FUE, proyecto.json ni Project.json en Data.");
                return;
            }

            _project = ProjectSerialization.Load(projectFile);
            if (string.IsNullOrEmpty(_project.ProjectDirectory))
                _project.ProjectDirectory = Path.GetDirectoryName(projectFile) ?? _dataDirectory;

            var objectsPath = _project.MainSceneObjectsPath;
            if (!File.Exists(objectsPath))
            {
                ShowFatal($"No existe el archivo de escena:\n{objectsPath}");
                return;
            }

            var layer = ObjectsSerialization.Load(objectsPath);
            var registry = ScriptSerialization.Load(_project.ScriptsPath);

            Title = string.IsNullOrWhiteSpace(_project.Nombre) ? "FUEngine" : _project.Nombre;
            _textureCache = new TextureAssetCache(_project.ProjectDirectory);
            TileMap? mapSnap = null;
            var mapPath = _project.GetSceneMapPath(0);
            if (!string.IsNullOrEmpty(mapPath) && File.Exists(mapPath))
            {
                try { mapSnap = MapSerialization.Load(mapPath); }
                catch { /* mapa opcional */ }
            }
            _runner = new PlayModeRunner(_project, layer, registry, () => { }, null, mapSnap, _playKeyboard);
            _runner.Start(useMainScene: false);

            int fps = Math.Clamp(_project.Fps, 15, 144);
            _renderTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.0 / fps) };
            _renderTimer.Tick += (_, _) => RenderFrame();
            _renderTimer.Start();

            GameCanvas.SizeChanged += (_, _) => RenderFrame();
            GameCanvas.MouseMove += (_, me) =>
            {
                var p = me.GetPosition(GameCanvas);
                _playKeyboard.MouseX = p.X;
                _playKeyboard.MouseY = p.Y;
            };
            GameCanvas.MouseLeftButtonDown += (_, _) => _playKeyboard.MouseLeft = true;
            GameCanvas.MouseLeftButtonUp += (_, _) => _playKeyboard.MouseLeft = false;
            RenderFrame();
        }
        catch (Exception ex)
        {
            ShowFatal(ex.ToString());
        }
    }

    private static string? FindProjectFile(string dataDir)
    {
        foreach (var name in new[] { NewProjectStructure.ProjectFileName, "proyecto.json", "Project.json" })
        {
            var p = Path.Combine(dataDir, name);
            if (File.Exists(p)) return p;
        }
        return null;
    }

    private void ShowFatal(string message)
    {
        TxtError.Text = message;
        System.Windows.MessageBox.Show(message, "FUEngine Player", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void RenderFrame()
    {
        if (_runner == null || !_runner.IsRunning || _project == null || _textureCache == null) return;
        double vw = GameCanvas.ActualWidth;
        double vh = GameCanvas.ActualHeight;
        if (vw <= 0 || vh <= 0) return;

        GameCanvas.Children.Clear();
        double? camX = null, camY = null;
        if (_runner.TryGetCameraCenterOverride(out var cx, out var cy))
        {
            camX = cx;
            camY = cy;
        }
        GameViewportRenderer.DrawWorldAndDebug(
            GameCanvas,
            _runner.GetSceneObjects(),
            _runner.GetDebugDrawSnapshot(),
            _project,
            _textureCache,
            vw,
            vh,
            _runner.GetPlayTileMap(),
            _runner.GameTimeSeconds,
            camX,
            camY);

        var ui = _runner.GetUiBackend();
        if (ui != null)
        {
            foreach (var entry in ui.GetLayoutEntries(vw, vh))
            {
                var vr = entry.ViewportRect;
                if (vr.Width < 0.5 || vr.Height < 0.5) continue;
                var fillColor = entry.Element.Kind switch
                {
                    UIElementKind.Button => System.Windows.Media.Color.FromArgb(120, 0x38, 0x8b, 0xfd),
                    UIElementKind.Text => System.Windows.Media.Color.FromArgb(100, 0xd2, 0x99, 0x22),
                    UIElementKind.Image => System.Windows.Media.Color.FromArgb(100, 0x2e, 0xa0, 0x43),
                    UIElementKind.Panel => System.Windows.Media.Color.FromArgb(60, 0x48, 0x4f, 0x58),
                    _ => System.Windows.Media.Color.FromArgb(80, 0xe6, 0xed, 0xf3)
                };
                var uiRect = new System.Windows.Shapes.Rectangle
                {
                    Width = vr.Width,
                    Height = vr.Height,
                    Fill = new System.Windows.Media.SolidColorBrush(fillColor),
                    Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(180, 0xe6, 0xed, 0xf3)),
                    StrokeThickness = 1
                };
                System.Windows.Controls.Canvas.SetLeft(uiRect, vr.X);
                System.Windows.Controls.Canvas.SetTop(uiRect, vr.Y);
                GameCanvas.Children.Add(uiRect);
            }
        }

        TxtStatus.Text = $"{_runner.CurrentFps:F0} fps · Esc · salir";
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
            return;
        }
        if (_runner != null && _runner.IsRunning && !_runner.IsPaused && ApplyPlayKeyInstance(e.Key, true))
            e.Handled = true;
    }

    private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (ApplyPlayKeyInstance(e.Key, false))
            e.Handled = true;
    }

    private bool ApplyPlayKeyInstance(Key key, bool down)
    {
        switch (key)
        {
            case Key.W: _playKeyboard.W = down; return true;
            case Key.A: _playKeyboard.A = down; return true;
            case Key.S: _playKeyboard.S = down; return true;
            case Key.D: _playKeyboard.D = down; return true;
            case Key.Left: _playKeyboard.Left = down; return true;
            case Key.Right: _playKeyboard.Right = down; return true;
            case Key.Up: _playKeyboard.Up = down; return true;
            case Key.Down: _playKeyboard.Down = down; return true;
            case Key.Space: _playKeyboard.Space = down; return true;
            case Key.E: _playKeyboard.E = down; return true;
            case Key.Q: _playKeyboard.Q = down; return true;
            case Key.F: _playKeyboard.F = down; return true;
            case Key.Enter: _playKeyboard.Enter = down; return true;
            case Key.LeftShift:
            case Key.RightShift:
                if (!down) _playKeyboard.Shift = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                else _playKeyboard.Shift = true;
                return true;
            case Key.LeftCtrl:
            case Key.RightCtrl:
                if (!down) _playKeyboard.Ctrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
                else _playKeyboard.Ctrl = true;
                return true;
            case Key.Escape: return false;
            default: return false;
        }
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _renderTimer?.Stop();
        _renderTimer = null;
        _playKeyboard.Clear();
        _runner?.Stop();
        _runner = null;
    }
}
