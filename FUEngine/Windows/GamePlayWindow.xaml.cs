using System.Windows;
using System.Windows.Threading;
using FUEngine.Core;
using FUEngine.Editor;
using FUEngine.Runtime;

namespace FUEngine;

/// <summary>
/// Ventana que ejecuta el juego al pulsar Play (Escena actual o Escena principal).
/// Al cerrar la ventana se detiene el PlayModeRunner.
/// </summary>
public partial class GamePlayWindow : Window
{
    private readonly ProjectInfo _project;
    private readonly ObjectLayer _objectLayer;
    private readonly ScriptRegistry _scriptRegistry;
    private readonly bool _useMainScene;
    private PlayModeRunner? _runner;
    private DispatcherTimer? _hudTimer;

    public GamePlayWindow(ProjectInfo project, ObjectLayer objectLayer, ScriptRegistry scriptRegistry, bool useMainScene)
    {
        InitializeComponent();
        _project = project;
        _objectLayer = objectLayer;
        _scriptRegistry = scriptRegistry;
        _useMainScene = useMainScene;
        TxtTitle.Text = $"Juego — {project.Nombre ?? "Proyecto"}";
        TxtScene.Text = useMainScene ? "Escena principal" : "Escena actual";
        Loaded += GamePlayWindow_Loaded;
    }

    private void GamePlayWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _runner = new PlayModeRunner(_project, _objectLayer, _scriptRegistry, () => { });
        _runner.Start(_useMainScene);
        _hudTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = System.TimeSpan.FromMilliseconds(250) };
        _hudTimer.Tick += HudTimer_Tick;
        _hudTimer.Start();
    }

    private void HudTimer_Tick(object? sender, System.EventArgs e)
    {
        if (_runner == null) return;
        TxtFps.Text = $" · FPS: {_runner.CurrentFps:F0}";
        TxtInfo.Text = $"Frame: {_runner.FrameCount} · Tiempo: {_runner.GameTimeSeconds:F1} s";
    }

    private void BtnStop_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _hudTimer?.Stop();
        _hudTimer = null;
        _runner?.Stop();
        _runner = null;
    }
}
