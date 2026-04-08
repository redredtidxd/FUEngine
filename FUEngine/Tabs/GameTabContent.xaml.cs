using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Color = System.Windows.Media.Color;
using Line = System.Windows.Shapes.Line;
using Ellipse = System.Windows.Shapes.Ellipse;
using Rectangle = System.Windows.Shapes.Rectangle;
using FUEngine.Core;
using FUEngine.Editor;
using FUEngine.Input;
using FUEngine.Rendering;
using FUEngine.Runtime;

namespace FUEngine;

/// <summary>
/// Tab "Juego" embebido: Play Mode aislado con su propio LuaScriptRuntime + WorldContextFromList.
/// Jerarquía de runtime + viewport + consola del tab; el inspector de objetos sigue en la pestaña Mapa.
/// </summary>
public partial class GameTabContent : System.Windows.Controls.UserControl, IDisposable
{
    private ProjectInfo? _project;
    private Func<ObjectLayer>? _getCurrentObjectLayer;
    private Func<TileMap>? _getCurrentTileMap;
    private Func<UIRoot>? _getCurrentUIRoot;
    private ScriptRegistry? _scriptRegistry;
    private PlayModeRunner? _runner;
    private readonly ObservableCollection<LogEntry> _tabLog = new();
    private readonly ObservableCollection<GameObject> _hierarchyObjects = new();
    private System.Windows.Threading.DispatcherTimer? _hudTimer;
    private ICollectionView? _logView;
    private GameObject? _contextMenuTarget;
    private TextureAssetCache? _textureCache;
    private readonly PlayKeyboardSnapshot _playKeyboard = new();
    private string? _lastAccessibilityHoverLinkId;
    private const int MaxTabLogEntries = 150;

    /// <summary>Si está activo y corriendo, el hot reload de scripts afecta solo a este tab.</summary>
    public bool IsActiveAndRunning => _runner != null && _runner.IsRunning;

    /// <summary>Runner del tab (para que el tab Debug pueda inspeccionar este runtime).</summary>
    public PlayModeRunner? GetRunner() => _runner;

    /// <summary>Fuerza un repintado del viewport (p. ej. tras mover el marco de cámara en el mapa).</summary>
    public void RefreshViewport() => RenderViewport();

    /// <summary>Tras modificar una imagen en disco desde el explorador (reescalar, etc.), invalida la caché de texturas de Play.</summary>
    public void NotifyProjectImageFileChanged(string? absolutePath)
    {
        _textureCache?.InvalidateAbsolutePath(absolutePath ?? "");
        RenderViewport();
    }

    /// <summary>Pausa el runtime cuando el tab se desactiva (evita que corra en segundo plano).</summary>
    public void PauseRunner()
    {
        if (_runner != null && _runner.IsRunning && !_runner.IsPaused)
            _runner.Pause();
    }

    /// <summary>Reanuda el runtime cuando el tab se vuelve a activar.</summary>
    public void ResumeRunner()
    {
        if (_runner != null && _runner.IsRunning && _runner.IsPaused)
            _runner.Resume();
    }

    /// <summary>Llamar cuando se guarda un .lua: recarga solo en este runtime (hot reload exclusivo del tab).</summary>
    public void OnScriptSaved(string relativePath)
    {
        _runner?.OnScriptSaved(relativePath ?? "");
    }

    /// <summary>Callback para aplicar el estado del sandbox a la escena del editor (transform + propiedades Lua → objetos.json).</summary>
    public Action<IReadOnlyList<RuntimeObjectState>, IReadOnlyList<RuntimeScriptPropertySnapshot>>? ApplyStateToScene { get; set; }

    public GameTabContent()
    {
        InitializeComponent();
        HierarchyList.ItemsSource = _hierarchyObjects;
        HierarchyList.DisplayMemberPath = "Name";
        _logView = CollectionViewSource.GetDefaultView(_tabLog);
        _logView.Filter = FilterLogEntry;
        LogList.ItemsSource = _logView;
    }

    private void GameTabContent_Loaded(object sender, RoutedEventArgs e)
    {
        Focusable = true;
    }

    private void GameTabContent_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (_runner == null || !_runner.IsRunning || _runner.IsPaused) return;
        if (ApplyPlayKey(e.Key, true)) e.Handled = true;
    }

    private void GameTabContent_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (ApplyPlayKey(e.Key, false)) e.Handled = true;
    }

    private bool ApplyPlayKey(Key key, bool down)
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
            case Key.E: _playKeyboard.E = down; return true;
            case Key.Q: _playKeyboard.Q = down; return true;
            case Key.F: _playKeyboard.F = down; return true;
            case Key.Enter: _playKeyboard.Enter = down; return true;
            case Key.LeftShift:
            case Key.RightShift:
                if (down) _playKeyboard.Shift = true;
                else _playKeyboard.Shift = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                return true;
            case Key.LeftCtrl:
            case Key.RightCtrl:
                if (down) _playKeyboard.Ctrl = true;
                else _playKeyboard.Ctrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
                return true;
            default: return false;
        }
    }

    private void GameTabContent_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Handled) return;
        if (e.Key == Key.Escape)
        {
            if (_runner != null && _runner.IsRunning)
            {
                BtnStop_OnClick(sender, e);
                e.Handled = true;
            }
            return;
        }
        if (e.Key == Key.R && (Keyboard.Modifiers & ModifierKeys.Control) == 0)
        {
            if (_runner != null && _runner.IsRunning)
            {
                BtnReset_OnClick(sender, e);
                e.Handled = true;
            }
            return;
        }
        if (e.Key == Key.Space || e.Key == Key.P)
        {
            if (_runner == null || !_runner.IsRunning) return;
            if (_runner.IsPaused)
            {
                BtnResume_OnClick(sender, e);
                e.Handled = true;
            }
            else
            {
                BtnPause_OnClick(sender, e);
                e.Handled = true;
            }
        }
    }

    private bool FilterLogEntry(object obj)
    {
        if (obj is not LogEntry e) return false;
        if (e.Level == LogLevel.Warning) return ChkFilterWarning?.IsChecked == true;
        if (e.Level == LogLevel.Error || e.Level == LogLevel.Critical) return ChkFilterError?.IsChecked == true;
        if (e.Level == LogLevel.Lua) return ChkFilterLua?.IsChecked == true;
        if (e.Level == LogLevel.Info) return ChkFilterInfo?.IsChecked == true;
        return true;
    }

    private void LogFilter_Changed(object sender, RoutedEventArgs e) => _logView?.Refresh();

    /// <summary>Inicializa el tab con el proyecto y la forma de obtener la escena actual (se clonará al iniciar).</summary>
    public void SetContext(ProjectInfo project, Func<ObjectLayer> getCurrentObjectLayer, ScriptRegistry scriptRegistry, Func<UIRoot>? getCurrentUIRoot = null, Func<TileMap>? getCurrentTileMap = null)
    {
        _project = project;
        _getCurrentObjectLayer = getCurrentObjectLayer;
        _getCurrentUIRoot = getCurrentUIRoot;
        _getCurrentTileMap = getCurrentTileMap;
        _scriptRegistry = scriptRegistry;
    }

    /// <summary>Actualiza la referencia al registro cuando scripts.json cambia (p. ej. nuevo script desde el explorador).</summary>
    public void UpdateScriptRegistry(ScriptRegistry? scriptRegistry) => _scriptRegistry = scriptRegistry;

    private void BtnStart_OnClick(object sender, RoutedEventArgs e)
    {
        if (_project == null || _getCurrentObjectLayer == null || _scriptRegistry == null) return;
        var layerCopy = ObjectsSerialization.Clone(_getCurrentObjectLayer());
        var uiRoot = _getCurrentUIRoot?.Invoke();
        var mapSnap = _getCurrentTileMap?.Invoke();
        _runner = new PlayModeRunner(_project, layerCopy, _scriptRegistry, () => { }, uiRoot, mapSnap, _playKeyboard);
        WirePlayViewportSurfaceProvider(_runner);
        _runner.Start(useMainScene: false);
        WireRuntimeToTabLog(_runner);

        _hierarchyObjects.Clear();
        foreach (var go in _runner.GetSceneObjects())
            _hierarchyObjects.Add(go);

        UpdatePlayButtons(running: true, paused: false);
        StartHudUpdates();
        AddLog(LogLevel.Info, $"Play iniciado en tab · {_runner.GetSceneObjects().Count} objetos.", "Juego");
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, new Action(() => Keyboard.Focus(this)));
        if (System.Windows.Window.GetWindow(this) is EditorWindow ed) ed.SyncDiscordRichPresence();
    }

    private void WirePlayViewportSurfaceProvider(PlayModeRunner runner)
    {
        runner.GetPlayViewportSurfacePixels = () =>
        {
            var c = GameViewportCanvas;
            if (c == null) return (800.0, 600.0);
            return (Math.Max(1.0, c.ActualWidth), Math.Max(1.0, c.ActualHeight));
        };
    }

    private void WireRuntimeToTabLog(PlayModeRunner runner)
    {
        var rt = runner.GetRuntime();
        if (rt != null)
        {
            var prevErr = rt.ScriptError;
            var prevPrint = rt.PrintOutput;
            rt.ScriptError = (path, line, msg) =>
            {
                prevErr?.Invoke(path, line, msg);
                var text = line > 0 ? $"{path}:{line} {msg}" : $"{path} {msg}";
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    AddLog(LogLevel.Error, text, "Lua", path, line > 0 ? line : null));
            };
            rt.PrintOutput = msg =>
            {
                prevPrint?.Invoke(msg);
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    AddLog(LogLevel.Lua, msg ?? "", "Lua"));
            };
        }
        var uiBackend = runner.GetUiBackend();
        if (uiBackend != null)
        {
            uiBackend.CallbackError = (canvasId, elementId, eventName, error) =>
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    var text = $"UI callback [{canvasId}/{elementId}/{eventName}]: {error}";
                    EditorLog.Error(text, "UI");
                    AddLog(LogLevel.Error, text, "UI");
                });
        }
    }

    /// <summary>Alinea la lista de jerarquía con <see cref="PlayModeRunner.GetSceneObjects"/> (objetos del mapa + creados por scripts en runtime).</summary>
    private void SyncHierarchyWithRuntime()
    {
        if (_runner == null || !_runner.IsRunning) return;
        var live = _runner.GetSceneObjects();
        var liveSet = new HashSet<GameObject>(live);
        for (int i = _hierarchyObjects.Count - 1; i >= 0; i--)
        {
            var go = _hierarchyObjects[i];
            if (!liveSet.Contains(go) || go.PendingDestroy)
                _hierarchyObjects.RemoveAt(i);
        }
        foreach (var go in live)
        {
            if (go.PendingDestroy) continue;
            if (!_hierarchyObjects.Contains(go))
                _hierarchyObjects.Add(go);
        }
    }

    private void RenderViewport()
    {
        var canvas = GameViewportCanvas;
        if (canvas == null) return;
        double vw = canvas.ActualWidth;
        double vh = canvas.ActualHeight;
        canvas.Children.Clear();
        if (vw <= 0 || vh <= 0) return;

        if (_runner == null || !_runner.IsRunning)
        {
            var msg = new TextBlock
            {
                Text = "▶  Viewport\nInicia el play para ver el mundo y los elementos UI.",
                Foreground = new SolidColorBrush(Color.FromRgb(0x6e, 0x76, 0x81)),
                FontSize = 11,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Width = 260,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(msg, (vw - 260) / 2);
            Canvas.SetTop(msg, vh / 2 - 24);
            canvas.Children.Add(msg);
            return;
        }

        SyncHierarchyWithRuntime();

        var objects = _runner.GetSceneObjects();
        _textureCache ??= new TextureAssetCache(_project?.ProjectDirectory);
        if (_project != null)
        {
            double? camX = null, camY = null;
            if (_runner.TryGetCameraCenterOverride(out var cx, out var cy))
            {
                camX = cx;
                camY = cy;
            }
            GameViewportRenderer.DrawWorldAndDebug(canvas, objects, _runner.GetDebugDrawSnapshot(), _project, _textureCache, vw, vh, _runner.GetPlayTileMap(), _runner.GameTimeSeconds, camX, camY);
        }

        var uiBackend = _runner.GetUiBackend();
        if (uiBackend != null)
        {
            uiBackend.ClearTextLinkHits();
            foreach (var entry in uiBackend.GetLayoutEntries(vw, vh))
            {
                var vr = entry.ViewportRect;
                if (vr.Width < 0.5 || vr.Height < 0.5) continue;

                var fillColor = entry.Element.Kind switch
                {
                    UIElementKind.Button => Color.FromArgb(120, 0x38, 0x8b, 0xfd),
                    UIElementKind.Text   => Color.FromArgb(100, 0xd2, 0x99, 0x22),
                    UIElementKind.Image  => Color.FromArgb(100, 0x2e, 0xa0, 0x43),
                    UIElementKind.Panel  => Color.FromArgb(60,  0x48, 0x4f, 0x58),
                    UIElementKind.TabControl => Color.FromArgb(95, 0xa3, 0x71, 0xf7),
                    _                   => Color.FromArgb(80,  0xe6, 0xed, 0xf3)
                };
                var uiRect = new Rectangle
                {
                    Width = vr.Width, Height = vr.Height,
                    Fill = new SolidColorBrush(fillColor),
                    Stroke = new SolidColorBrush(Color.FromArgb(180, 0xe6, 0xed, 0xf3)),
                    StrokeThickness = 1,
                    ToolTip = $"{entry.Element.Kind}: {entry.Element.Id}"
                };
                Canvas.SetLeft(uiRect, vr.X);
                Canvas.SetTop(uiRect, vr.Y);
                canvas.Children.Add(uiRect);

                var hasText = !string.IsNullOrEmpty(entry.Element.Text) || !string.IsNullOrEmpty(entry.Element.LocalizationKey);
                if (entry.Element.Kind is UIElementKind.Text or UIElementKind.Button &&
                    hasText && vr.Width > 4 && _project != null)
                {
                    var ppd = VisualTreeHelper.GetDpi(canvas).PixelsPerDip;
                    var loc = _runner.GetLocalizationRuntime();
                    var resolvedTw = UiTextResolve.Resolve(entry.Element, _project.ProjectDirectory, loc).Typewriter;
                    var textBuild = UiTextRenderer.Build(new UiTextRenderer.RenderArgs
                    {
                        Element = entry.Element,
                        CanvasRect = entry.CanvasRect,
                        ProjectRoot = _project.ProjectDirectory,
                        PixelsPerDip = ppd,
                        GameTimeSeconds = _runner.GameTimeSeconds,
                        Localization = loc,
                        VisiblePlainCharCount = _runner.UiTypewriter.GetVisiblePlainLength(entry.CanvasId, entry.Element, _project.ProjectDirectory, loc),
                        CharRevealGameTimes = _runner.UiTypewriter.GetCharRevealTimes(entry.CanvasId, entry.Element, _project.ProjectDirectory, loc),
                        TypewriterFadeInActive = resolvedTw?.Enabled == true && resolvedTw.FadeInPerChar,
                        FadeInDurationSeconds = resolvedTw?.FadeInDurationSeconds ?? 0.08
                    });
                    if (textBuild != null)
                    {
                        var fe = textBuild.Root;
                        fe.IsHitTestVisible = false;
                        var cw = Math.Max(1, entry.CanvasRect.Width);
                        var ch = Math.Max(1, entry.CanvasRect.Height);
                        fe.Width = cw;
                        fe.Height = ch;
                        fe.LayoutTransform = new ScaleTransform(vr.Width / cw, vr.Height / ch);
                        Canvas.SetLeft(fe, vr.X);
                        Canvas.SetTop(fe, vr.Y);
                        canvas.Children.Add(fe);
                        if (!string.IsNullOrWhiteSpace(entry.Element.Id) && textBuild.LinkRects.Count > 0)
                        {
                            var pad = UiTextRenderer.InnerPadding;
                            var sc = textBuild.ContentLayoutScale;
                            var hits = new List<UiTextLinkHit>(textBuild.LinkRects.Count);
                            foreach (var lr in textBuild.LinkRects)
                            {
                                hits.Add(new UiTextLinkHit(lr.LinkId, new UIRect
                                {
                                    X = entry.CanvasRect.X + pad + lr.X * sc,
                                    Y = entry.CanvasRect.Y + pad + lr.Y * sc,
                                    Width = lr.Width * sc,
                                    Height = lr.Height * sc
                                }));
                            }
                            uiBackend.SetTextLinkHits(entry.CanvasId, entry.Element.Id, hits);
                        }
                    }
                }
            }
        }

        var fpsLbl = new TextBlock
        {
            Text = $"▶ {_runner.CurrentFps:F0} fps  obj:{objects.Count}",
            FontSize = 9,
            Foreground = new SolidColorBrush(Color.FromArgb(200, 0x58, 0xa6, 0xff)),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(fpsLbl, 6);
        Canvas.SetTop(fpsLbl, 4);
        canvas.Children.Add(fpsLbl);

        if (_runner.IsPaused)
        {
            var pauseMsg = new TextBlock
            {
                Text = "⏸ PAUSADO",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromArgb(220, 0xd2, 0x99, 0x22)),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(pauseMsg, vw / 2 - 50);
            Canvas.SetTop(pauseMsg, 8);
            canvas.Children.Add(pauseMsg);
        }
    }

    private void ViewportCanvas_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_runner == null || !_runner.IsRunning) return;
        var pos = e.GetPosition(GameViewportCanvas);
        _playKeyboard.MouseLeft = true;
        _playKeyboard.MouseX = pos.X;
        _playKeyboard.MouseY = pos.Y;
        bool consumed = DispatchViewportPointerEvent(pos.X, pos.Y, "pressed");
        if (!consumed) consumed = DispatchViewportPointerEvent(pos.X, pos.Y, "click");
        if (consumed) e.Handled = true;
    }

    private void ViewportCanvas_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_runner == null || !_runner.IsRunning) return;
        var pos = e.GetPosition(GameViewportCanvas);
        _playKeyboard.MouseLeft = false;
        _playKeyboard.MouseX = pos.X;
        _playKeyboard.MouseY = pos.Y;
        bool consumed = DispatchViewportPointerEvent(pos.X, pos.Y, "released");
        if (consumed) e.Handled = true;
    }

    private void ViewportCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_runner == null || !_runner.IsRunning) return;
        var pos = e.GetPosition(GameViewportCanvas);
        _playKeyboard.MouseX = pos.X;
        _playKeyboard.MouseY = pos.Y;
        var vw = GameViewportCanvas.ActualWidth;
        var vh = GameViewportCanvas.ActualHeight;
        var ui = _runner.GetUiBackend();
        ui?.DispatchPointerEvent(pos.X, pos.Y, vw, vh, "hover");

        if (EngineSettings.Load().UiAccessibilityTtsEnabled &&
            ui != null &&
            ui.TryHitTest(pos.X, pos.Y, vw, vh, out var hit) &&
            !string.IsNullOrEmpty(hit.TextLinkId))
        {
            if (!string.Equals(_lastAccessibilityHoverLinkId, hit.TextLinkId, StringComparison.Ordinal))
            {
                _lastAccessibilityHoverLinkId = hit.TextLinkId;
                _runner.AccessibilityTts?.Speak($"Enlace: {hit.TextLinkId}", interruptPrevious: true);
            }
        }
        else
            _lastAccessibilityHoverLinkId = null;
    }

    /// <summary>Envía evento de puntero a la UI (hit-test + bindings). Devuelve true si la UI consumió el input (BlocksInput).</summary>
    private bool DispatchViewportPointerEvent(double x, double y, string eventName)
    {
        var uiBackend = _runner?.GetUiBackend();
        if (uiBackend == null) return false;
        bool consumed = uiBackend.DispatchPointerEvent(
            x, y,
            GameViewportCanvas.ActualWidth, GameViewportCanvas.ActualHeight,
            eventName);
        AddLog(LogLevel.Info,
            consumed
                ? $"[UI] {eventName} ({x:F0},{y:F0}) → consumido"
                : $"[Input] {eventName} ({x:F0},{y:F0}) sin hit en UI",
            "Viewport");
        return consumed;
    }

    private void AddLog(LogLevel level, string message, string? source, string? filePath = null, int? line = null)
    {
        var entry = new LogEntry
        {
            Time = DateTime.Now,
            Level = level,
            Message = message,
            Source = source,
            FilePath = filePath,
            Line = line
        };
        _tabLog.Add(entry);
        while (_tabLog.Count > MaxTabLogEntries)
            _tabLog.RemoveAt(0);
        if (LogList.Items.Count > 0)
            LogList.ScrollIntoView(LogList.Items[LogList.Items.Count - 1]);
    }

    private void BtnStop_OnClick(object sender, RoutedEventArgs e)
    {
        StopRunner();
        UpdatePlayButtons(running: false, paused: false);
        StopHudUpdates();
        _hierarchyObjects.Clear();
        _textureCache?.Clear();
        RenderViewport();
        AddLog(LogLevel.Info, "Play detenido. Runtime destruido.", "Juego");
        if (System.Windows.Window.GetWindow(this) is EditorWindow ed2) ed2.SyncDiscordRichPresence();
    }

    private void StopRunner()
    {
        _playKeyboard.Clear();
        _runner?.Stop();
        _runner = null;
    }

    private void BtnPause_OnClick(object sender, RoutedEventArgs e)
    {
        if (_runner == null) return;
        _runner.Pause();
        UpdatePlayButtons(running: true, paused: true);
        AddLog(LogLevel.Info, "Juego pausado.", "Juego");
    }

    private void BtnResume_OnClick(object sender, RoutedEventArgs e)
    {
        if (_runner == null) return;
        _runner.Resume();
        UpdatePlayButtons(running: true, paused: false);
        AddLog(LogLevel.Info, "Juego reanudado.", "Juego");
    }

    private void BtnReset_OnClick(object sender, RoutedEventArgs e)
    {
        StopRunner();
        UpdatePlayButtons(running: false, paused: false);
        StopHudUpdates();
        _hierarchyObjects.Clear();

        if (_project == null || _getCurrentObjectLayer == null || _scriptRegistry == null) return;
        var layerCopy = ObjectsSerialization.Clone(_getCurrentObjectLayer());
        var uiRoot = _getCurrentUIRoot?.Invoke();
        var mapSnap2 = _getCurrentTileMap?.Invoke();
        _runner = new PlayModeRunner(_project, layerCopy, _scriptRegistry, () => { }, uiRoot, mapSnap2, _playKeyboard);
        WirePlayViewportSurfaceProvider(_runner);
        _runner.Start(useMainScene: false);
        WireRuntimeToTabLog(_runner);

        foreach (var go in _runner.GetSceneObjects())
            _hierarchyObjects.Add(go);

        UpdatePlayButtons(running: true, paused: false);
        StartHudUpdates();
        AddLog(LogLevel.Info, "Tab reiniciado con escena actual.", "Juego");
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, new Action(() => Keyboard.Focus(this)));
    }

    private void UpdatePlayButtons(bool running, bool paused)
    {
        if (BtnStart != null) BtnStart.IsEnabled = !running;
        if (BtnStop != null) BtnStop.IsEnabled = running;
        if (BtnPause != null)
        {
            BtnPause.IsEnabled = running && !paused;
            BtnPause.Visibility = paused ? Visibility.Collapsed : Visibility.Visible;
        }
        if (BtnResume != null)
        {
            BtnResume.IsEnabled = running && paused;
            BtnResume.Visibility = paused ? Visibility.Visible : Visibility.Collapsed;
        }
        if (PanelHud != null) PanelHud.Visibility = running ? Visibility.Visible : Visibility.Collapsed;
        if (BtnSaveState != null) BtnSaveState.IsEnabled = running;
    }

    private void BtnSaveState_OnClick(object sender, RoutedEventArgs e)
    {
        if (_runner == null || ApplyStateToScene == null) return;
        var states = _runner.GetObjectStates();
        if (states.Count == 0) { AddLog(LogLevel.Info, "No hay objetos en el runtime para guardar.", "Juego"); return; }
        var scriptSnaps = _runner.GetScriptPropertySnapshotsForScene();
        ApplyStateToScene(states, scriptSnaps);
        AddLog(LogLevel.Info, $"Escena: {states.Count} objetos + {scriptSnaps.Count} valores de script persistidos en disco.", "Juego");
    }

    private void StartHudUpdates()
    {
        int fps = Math.Clamp(_project?.Fps ?? 60, 15, 144);
        _hudTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1.0 / fps)
        };
        _hudTimer.Tick += (_, _) => UpdateHud();
        _hudTimer.Start();
    }

    private void StopHudUpdates()
    {
        _hudTimer?.Stop();
        _hudTimer = null;
    }

    private void UpdateHud()
    {
        if (_runner == null || TxtFps == null) return;
        TxtFps.Text = $"FPS: {_runner.CurrentFps:F0}";
        TxtFrame.Text = $"Frame: {_runner.FrameCount}";
        TxtDelta.Text = $"dt: {_runner.LastDeltaTimeSeconds * 1000:F1} ms";
        TxtTime.Text = $"Tiempo: {_runner.GameTimeSeconds:F1} s";
        if (TxtObjectCount != null) TxtObjectCount.Text = $"Objetos: {_runner.GetSceneObjects().Count}";
        RenderViewport();
    }

    private void ChkHudAdvanced_Changed(object sender, RoutedEventArgs e)
    {
        if (HudAdvanced != null)
            HudAdvanced.Visibility = ChkHudAdvanced?.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }

    private void HierarchyList_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var listBox = (System.Windows.Controls.ListBox)sender;
        var element = e.OriginalSource as System.Windows.FrameworkElement;
        while (element != null)
        {
            var item = element as System.Windows.Controls.ListBoxItem ?? (element.TemplatedParent as System.Windows.Controls.ListBoxItem);
            if (item != null && item.DataContext is GameObject go)
            {
                _contextMenuTarget = go;
                listBox.SelectedItem = go;
                break;
            }
            element = System.Windows.Media.VisualTreeHelper.GetParent(element) as System.Windows.FrameworkElement;
        }
    }

    private void HierarchyList_ContextMenuOpening(object? sender, ContextMenuEventArgs e)
    {
        var go = HierarchyList.SelectedItem as GameObject ?? _contextMenuTarget;
        if (MenuHierarchyPrint != null) MenuHierarchyPrint.IsEnabled = go != null;
        if (MenuHierarchyDestroy != null) MenuHierarchyDestroy.IsEnabled = go != null && _runner != null && _runner.IsRunning;
    }

    private void MenuHierarchyPrint_OnClick(object sender, RoutedEventArgs e)
    {
        var go = HierarchyList.SelectedItem as GameObject ?? _contextMenuTarget;
        if (go == null) return;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[{go.Name ?? "(sin nombre)"}]");
        if (go.Transform != null)
        {
            sb.AppendLine($"  Transform: X={go.Transform.X:F2} Y={go.Transform.Y:F2} Rot={go.Transform.RotationDegrees:F1}° Scale=({go.Transform.ScaleX:F2},{go.Transform.ScaleY:F2})");
        }
        if (go.Components != null)
        {
            foreach (var c in go.Components)
            {
                if (c is ScriptComponent sc)
                    sb.AppendLine($"  Script: {sc.ScriptId ?? "(sin id)"}");
                else
                    sb.AppendLine($"  {c.GetType().Name}");
            }
        }
        AddLog(LogLevel.Info, sb.ToString().TrimEnd(), "Debug");
    }

    private void MenuHierarchyDestroy_OnClick(object sender, RoutedEventArgs e)
    {
        var go = HierarchyList.SelectedItem as GameObject ?? _contextMenuTarget;
        if (go == null || _runner == null) return;
        var name = go.Name ?? "(sin nombre)";
        _runner.DestroyObject(go);
        _hierarchyObjects.Remove(go);
        if (HierarchyList.SelectedItem == go) HierarchyList.SelectedItem = null;
        AddLog(LogLevel.Info, $"Objeto '{name}' detenido/eliminado del runtime.", "Juego");
    }

    private void BtnClearLog_OnClick(object sender, RoutedEventArgs e)
    {
        _tabLog.Clear();
    }

    private void LogList_OnMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (LogList.SelectedItem is not LogEntry entry || string.IsNullOrEmpty(entry.FilePath) || !entry.Line.HasValue)
            return;
        RequestOpenFileAtLine?.Invoke(this, (entry.FilePath, entry.Line.Value));
    }

    /// <summary>Se dispara al hacer doble clic en un error con archivo/línea para abrir el script en el editor.</summary>
    public event EventHandler<(string FilePath, int Line)>? RequestOpenFileAtLine;

    public void Dispose()
    {
        StopRunner();
        StopHudUpdates();
        _hierarchyObjects.Clear();
        _tabLog.Clear();
        _project = null;
        _getCurrentObjectLayer = null;
        _scriptRegistry = null;
    }
}
