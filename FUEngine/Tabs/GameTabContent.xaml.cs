using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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
/// Ejecuta una copia de la escena actual; al cerrar el tab se hace Dispose limpio.
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
    private const int MaxTabLogEntries = 150;

    /// <summary>Si está activo y corriendo, el hot reload de scripts afecta solo a este tab.</summary>
    public bool IsActiveAndRunning => _runner != null && _runner.IsRunning;

    /// <summary>Runner del tab (para que el tab Debug pueda inspeccionar este runtime).</summary>
    public PlayModeRunner? GetRunner() => _runner;

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
        if (e.Level == LogLevel.Error) return ChkFilterError?.IsChecked == true;
        if (e.Level == LogLevel.Info)
        {
            if (string.Equals(e.Source, "Lua", StringComparison.OrdinalIgnoreCase))
                return ChkFilterLua?.IsChecked == true;
            return ChkFilterInfo?.IsChecked == true;
        }
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
        _runner.Start(useMainScene: false);
        WireRuntimeToTabLog(_runner);

        _hierarchyObjects.Clear();
        foreach (var go in _runner.GetSceneObjects())
            _hierarchyObjects.Add(go);

        UpdatePlayButtons(running: true, paused: false);
        StartHudUpdates();
        AddLog(LogLevel.Info, $"Play iniciado en tab · {_runner.GetSceneObjects().Count} objetos.", "Juego");
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, new Action(() => Keyboard.Focus(this)));
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
                    AddLog(LogLevel.Info, msg ?? "", "Lua"));
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

                if (!string.IsNullOrEmpty(entry.Element.Text) && vr.Width > 8)
                {
                    var uiText = new TextBlock
                    {
                        Text = entry.Element.Text,
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Colors.White),
                        IsHitTestVisible = false,
                        MaxWidth = vr.Width - 8,
                        TextWrapping = TextWrapping.NoWrap,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };
                    Canvas.SetLeft(uiText, vr.X + 4);
                    Canvas.SetTop(uiText, vr.Y + 4);
                    canvas.Children.Add(uiText);
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
        _runner.GetUiBackend()?.DispatchPointerEvent(
            pos.X, pos.Y,
            GameViewportCanvas.ActualWidth, GameViewportCanvas.ActualHeight,
            "hover");
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
        InspectorContent.Children.Clear();
        InspectorTitle.Text = "Inspector — Selecciona un objeto";
        _textureCache?.Clear();
        RenderViewport();
        AddLog(LogLevel.Info, "Play detenido. Runtime destruido.", "Juego");
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
        InspectorContent.Children.Clear();
        InspectorTitle.Text = "Inspector — Selecciona un objeto";

        if (_project == null || _getCurrentObjectLayer == null || _scriptRegistry == null) return;
        var layerCopy = ObjectsSerialization.Clone(_getCurrentObjectLayer());
        var uiRoot = _getCurrentUIRoot?.Invoke();
        var mapSnap2 = _getCurrentTileMap?.Invoke();
        _runner = new PlayModeRunner(_project, layerCopy, _scriptRegistry, () => { }, uiRoot, mapSnap2, _playKeyboard);
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
        if (MenuHierarchyFocus != null) MenuHierarchyFocus.IsEnabled = go != null;
        if (MenuHierarchyPrint != null) MenuHierarchyPrint.IsEnabled = go != null;
        if (MenuHierarchyDestroy != null) MenuHierarchyDestroy.IsEnabled = go != null && _runner != null && _runner.IsRunning;
    }

    private void MenuHierarchyFocus_OnClick(object sender, RoutedEventArgs e)
    {
        if (HierarchyList.SelectedItem is GameObject go)
        {
            InspectorContent.Children.Clear();
            InspectorTitle.Text = $"Inspector — {go.Name ?? "(sin nombre)"}";
            BuildInspectorFor(go);
        }
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
        InspectorContent.Children.Clear();
        InspectorTitle.Text = "Inspector — Selecciona un objeto";
        AddLog(LogLevel.Info, $"Objeto '{name}' detenido/eliminado del runtime.", "Juego");
    }

    private void HierarchyList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        InspectorContent.Children.Clear();
        if (HierarchyList.SelectedItem is not GameObject go)
        {
            InspectorTitle.Text = "Inspector — Selecciona un objeto";
            return;
        }
        InspectorTitle.Text = $"Inspector — {go.Name ?? "(sin nombre)"}";
        BuildInspectorFor(go);
    }

    private void BuildInspectorFor(GameObject go)
    {
        var brush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xe6, 0xed, 0xf3));
        var brushLabel = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x8b, 0x94, 0x9e));

        void AddRow(string label, string value)
        {
            var sp = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 4) };
            sp.Children.Add(new TextBlock { Text = label + ":", Foreground = brushLabel, FontSize = 11, Width = 90 });
            sp.Children.Add(new TextBlock { Text = value, Foreground = brush, FontSize = 11, TextWrapping = TextWrapping.Wrap });
            InspectorContent.Children.Add(sp);
        }

        AddRow("Nombre", go.Name ?? "(sin nombre)");
        if (go.Transform != null)
        {
            AddRow("X", go.Transform.X.ToString("F2"));
            AddRow("Y", go.Transform.Y.ToString("F2"));
            AddRow("Rotación", go.Transform.RotationDegrees.ToString("F1") + "°");
            AddRow("ScaleX", go.Transform.ScaleX.ToString("F2"));
            AddRow("ScaleY", go.Transform.ScaleY.ToString("F2"));
        }
        if (go.Components != null && go.Components.Count > 0)
        {
            InspectorContent.Children.Add(new TextBlock { Text = "Componentes", Foreground = brushLabel, FontSize = 11, Margin = new Thickness(0, 8, 0, 4) });
            foreach (var c in go.Components)
            {
                if (c is ScriptComponent sc)
                {
                    AddRow("  Script", sc.ScriptId ?? "(sin id)");
                    if (sc.ScriptInstanceHandle is ScriptInstance si)
                        AddRuntimeLuaVariablesSection(si, brush, brushLabel);
                }
                else
                    AddRow("  " + c.GetType().Name, "");
            }
        }
    }

    private void AddRuntimeLuaVariablesSection(ScriptInstance si, SolidColorBrush valueBrush, SolidColorBrush labelBrush)
    {
        InspectorContent.Children.Add(new TextBlock
        {
            Text = "  Variables Lua (en vivo)",
            Foreground = labelBrush,
            FontSize = 11,
            Margin = new Thickness(0, 6, 0, 2)
        });
        var snap = si.GetVariableSnapshot();
        if (snap == null || snap.Count == 0)
        {
            InspectorContent.Children.Add(new TextBlock
            {
                Text = "    (vacío)",
                Foreground = labelBrush,
                FontSize = 10,
                Margin = new Thickness(0, 0, 0, 4)
            });
            return;
        }

        foreach (var kv in snap.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (KnownEvents.IsReservedScriptVariableName(kv.Key)) continue;
            if (kv.Value.StartsWith("table:", StringComparison.OrdinalIgnoreCase)) continue;

            var row = new Grid { Margin = new Thickness(8, 2, 0, 2) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(96) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var lbl = new TextBlock
            {
                Text = kv.Key,
                Foreground = labelBrush,
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            var tb = new System.Windows.Controls.TextBox
            {
                Text = kv.Value,
                Foreground = valueBrush,
                FontSize = 10,
                Background = new SolidColorBrush(Color.FromRgb(0x0d, 0x11, 0x17)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x30, 0x36, 0x3d)),
                Padding = new Thickness(4, 2, 4, 2),
                CaretBrush = valueBrush,
                Tag = Tuple.Create(si, kv.Key)
            };
            tb.LostFocus += LuaRuntimeVar_LostFocus;
            Grid.SetColumn(lbl, 0);
            Grid.SetColumn(tb, 1);
            row.Children.Add(lbl);
            row.Children.Add(tb);
            InspectorContent.Children.Add(row);
        }
    }

    private void LuaRuntimeVar_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox tb || tb.Tag is not Tuple<ScriptInstance, string> tup) return;
        TryApplyLuaVariable(tup.Item1, tup.Item2, tb.Text ?? "");
        var cur = tup.Item1.Get(tup.Item2);
        tb.Text = cur == null ? "nil" : cur.ToString() ?? "nil";
    }

    private static void TryApplyLuaVariable(ScriptInstance si, string key, string text)
    {
        var t = text.Trim();
        if (string.Equals(t, "nil", StringComparison.OrdinalIgnoreCase))
        {
            si.Set(key, null);
            return;
        }

        if (bool.TryParse(t, out var b))
        {
            si.Set(key, b);
            return;
        }

        if (long.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out var lg) && t.IndexOf('.') < 0)
        {
            si.Set(key, (double)lg);
            return;
        }

        if (double.TryParse(t, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
        {
            si.Set(key, d);
            return;
        }

        si.Set(key, t);
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
