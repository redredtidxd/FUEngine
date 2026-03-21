using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FUEngine.Core;
using FUEngine.Runtime;

namespace FUEngine;

/// <summary>
/// Tab Debug: introspección del runtime activo (variables de scripts, objetos, estado del motor).
/// Consola = logs; Debug = ver qué está pasando dentro del juego.
/// </summary>
public partial class DebugTabContent : System.Windows.Controls.UserControl
{
    private System.Func<PlayModeRunner?>? _getCurrentRunner;
    private System.Windows.Threading.DispatcherTimer? _refreshTimer;
    private PlayModeRunner? _lastRunner;
    private readonly List<string> _eventLog = new();
    private const int RefreshIntervalMs = 200;
    private const int MaxEventLogEntries = 50;
    private readonly List<(string path, int line)> _breakpoints = new();

    public DebugTabContent()
    {
        InitializeComponent();
    }

    /// <summary>Asigna la forma de obtener el runtime a inspeccionar (Play global o tab Juego activo).</summary>
    public void SetContext(System.Func<PlayModeRunner?> getCurrentRunner)
    {
        _getCurrentRunner = getCurrentRunner;
        UnsubscribeEventExecuting();
        _lastRunner = null;
        _refreshTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = System.TimeSpan.FromMilliseconds(RefreshIntervalMs)
        };
        _refreshTimer.Tick += (_, _) => Refresh();
        _refreshTimer.Start();
        Refresh();
    }

    private void SubscribeEventExecuting(PlayModeRunner runner)
    {
        if (runner == _lastRunner) return;
        var rt = runner.GetRuntime();
        if (rt == null) return;
        UnsubscribeEventExecuting();
        _lastRunner = runner;
        rt.EventExecuting += OnEventExecuting;
    }

    private void UnsubscribeEventExecuting()
    {
        if (_lastRunner == null) return;
        var rt = _lastRunner.GetRuntime();
        if (rt != null)
            rt.EventExecuting -= OnEventExecuting;
        _lastRunner = null;
    }

    private void OnEventExecuting(ScriptInstance inst, string eventName)
    {
        var entry = $"{eventName}: {inst.ScriptPath}";
        lock (_eventLog)
        {
            _eventLog.Add(entry);
            while (_eventLog.Count > MaxEventLogEntries)
                _eventLog.RemoveAt(0);
        }
    }

    private void Refresh()
    {
        var runner = _getCurrentRunner?.Invoke();
        if (runner == null || !runner.IsRunning)
        {
            UnsubscribeEventExecuting();
            if (TxtRuntimeSource != null) TxtRuntimeSource.Text = "Sin runtime activo";
            if (TxtFps != null) TxtFps.Text = "FPS: —";
            if (TxtFrame != null) TxtFrame.Text = "Frame: 0";
            if (TxtDelta != null) TxtDelta.Text = "dt: — ms";
            if (TxtObjects != null) TxtObjects.Text = "Objetos: 0";
            if (TxtScripts != null) TxtScripts.Text = "Scripts: 0";
            if (TxtLuaMem != null) TxtLuaMem.Text = "Lua: — KB";
            if (TxtMemory != null) TxtMemory.Text = "Memory: —";
            if (TxtDrawCalls != null) TxtDrawCalls.Text = "Draw: —";
            if (TxtAudioChannels != null) TxtAudioChannels.Text = "Audio: —";
            if (TxtBreakpointPaused != null) TxtBreakpointPaused.Visibility = Visibility.Collapsed;
            if (BtnResumeBreakpoint != null) BtnResumeBreakpoint.Visibility = Visibility.Collapsed;
            if (SceneObjectsList != null) SceneObjectsList.ItemsSource = null;
            if (EventosList != null) EventosList.ItemsSource = null;
            if (ColisionesList != null) ColisionesList.ItemsSource = null;
            InspectorContent?.Children.Clear();
            ScriptVarsContent?.Children.Clear();
            if (InspectorTitle != null) InspectorTitle.Text = "Inspector — Inicia Play o un tab Juego";
            return;
        }

        SubscribeEventExecuting(runner);

        if (TxtRuntimeSource != null) TxtRuntimeSource.Text = "Runtime activo";
        if (TxtFps != null) TxtFps.Text = $"FPS: {runner.CurrentFps:F0}";
        if (TxtFrame != null) TxtFrame.Text = $"Frame: {runner.FrameCount}";
        if (TxtDelta != null) TxtDelta.Text = $"dt: {runner.LastDeltaTimeSeconds * 1000:F1} ms";
        if (TxtObjects != null) TxtObjects.Text = $"Objetos: {runner.GetSceneObjects().Count}";
        if (TxtScripts != null) TxtScripts.Text = $"Scripts: {runner.GetActiveScriptCount()}";
        if (TxtLuaMem != null) TxtLuaMem.Text = $"Lua: {runner.GetLuaMemoryKb():F1} KB";
        try
        {
            var proc = Process.GetCurrentProcess();
            if (TxtMemory != null) TxtMemory.Text = $"Memory: {proc.WorkingSet64 / (1024 * 1024)} MB";
        }
        catch { if (TxtMemory != null) TxtMemory.Text = "Memory: —"; }
        if (TxtDrawCalls != null) TxtDrawCalls.Text = "Draw: —";
        if (TxtAudioChannels != null) TxtAudioChannels.Text = "Audio: —";

        if (runner.IsPausedForBreakpoint)
        {
            var (path, line) = runner.GetBreakpointLocation();
            if (TxtBreakpointPaused != null) TxtBreakpointPaused.Text = $"Pausado en {path}:{line}";
            if (TxtBreakpointPaused != null) TxtBreakpointPaused.Visibility = Visibility.Visible;
            if (BtnResumeBreakpoint != null) BtnResumeBreakpoint.Visibility = Visibility.Visible;
        }
        else
        {
            if (TxtBreakpointPaused != null) TxtBreakpointPaused.Visibility = Visibility.Collapsed;
            if (BtnResumeBreakpoint != null) BtnResumeBreakpoint.Visibility = Visibility.Collapsed;
        }

        if (_breakpoints.Count > 0)
        {
            runner.SetBreakpoints(_breakpoints);
            if (BreakpointsList != null)
                BreakpointsList.ItemsSource = _breakpoints.Select(b => $"{b.path}:{b.line}").ToList();
        }
        else if (BreakpointsList != null)
            BreakpointsList.ItemsSource = null;

        var objects = runner.GetSceneObjects();
        if (SceneObjectsList != null && SceneObjectsList.ItemsSource != objects)
            SceneObjectsList.ItemsSource = objects;

        string[] eventSnapshot;
        lock (_eventLog)
            eventSnapshot = _eventLog.ToArray();
        if (EventosList != null)
            EventosList.ItemsSource = eventSnapshot.Length > 0 ? eventSnapshot.Reverse().ToArray() : null;

        BuildColisionesList(runner);

        var selected = SceneObjectsList?.SelectedItem as GameObject;
        if (selected == null)
        {
            InspectorContent?.Children.Clear();
            ScriptVarsContent?.Children.Clear();
            if (InspectorTitle != null) InspectorTitle.Text = "Inspector — Selecciona un objeto";
            return;
        }

        BuildInspector(selected);
        BuildScriptVariables(runner, selected);
    }

    private void BuildColisionesList(PlayModeRunner runner)
    {
        var showColliders = ChkShowColliders?.IsChecked == true;
        var showHitboxes = ChkShowHitboxes?.IsChecked == true;
        var showTriggers = ChkShowTriggers?.IsChecked == true;
        var list = new List<string>();
        foreach (var go in runner.GetSceneObjects())
        {
            if (go == null) continue;
            var col = go.GetComponent<ColliderComponent>();
            if (col != null)
            {
                if (col.IsTrigger && !showTriggers) continue;
                if (!col.IsTrigger && !showColliders && !showHitboxes) continue;
                var t = go.Transform;
                var x = t?.X ?? 0;
                var y = t?.Y ?? 0;
                var w = col.Width;
                var h = col.Height;
                var kind = col.IsTrigger ? "Trigger" : "Collider";
                list.Add($"{go.Name}: {kind} ({x:F0},{y:F0}) {w}x{h}");
            }
            else if (showHitboxes && go.Transform != null)
            {
                var t = go.Transform;
                list.Add($"{go.Name ?? "(sin nombre)"}: Bounds ({t.X:F0},{t.Y:F0})");
            }
        }
        if (ColisionesList != null)
            ColisionesList.ItemsSource = list.Count > 0 ? list : null;
    }

    private void CollisionFilter_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        Refresh();
    }

    private void BtnAddBreakpoint_OnClick(object sender, RoutedEventArgs e)
    {
        var path = TxtBreakpointPath?.Text?.Trim();
        if (string.IsNullOrEmpty(path)) return;
        if (!int.TryParse(TxtBreakpointLine?.Text?.Trim(), out var line) || line < 1) line = 1;
        _breakpoints.Add((path, line));
        var runner = _getCurrentRunner?.Invoke();
        runner?.SetBreakpoints(_breakpoints);
        BreakpointsList.ItemsSource = null;
        BreakpointsList.ItemsSource = _breakpoints.Select(b => $"{b.path}:{b.line}").ToList();
    }

    private void BtnResumeBreakpoint_OnClick(object sender, RoutedEventArgs e)
    {
        var runner = _getCurrentRunner?.Invoke();
        runner?.ResumeFromBreakpoint();
    }

    private void BuildInspector(GameObject go)
    {
        InspectorContent.Children.Clear();
        if (InspectorTitle != null) InspectorTitle.Text = $"Inspector — {go.Name ?? "(sin nombre)"}";

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
            AddRow("Position", $"{go.Transform.X:F2}, {go.Transform.Y:F2}");
            AddRow("Rotation", go.Transform.RotationDegrees.ToString("F1") + "°");
            AddRow("Scale", $"{go.Transform.ScaleX:F2}, {go.Transform.ScaleY:F2}");
        }
        if (go.Components != null && go.Components.Count > 0)
        {
            InspectorContent.Children.Add(new TextBlock { Text = "Components", Foreground = brushLabel, FontSize = 11, Margin = new Thickness(0, 8, 0, 4) });
            foreach (var c in go.Components)
            {
                if (c is ScriptComponent sc)
                    AddRow("  Script", sc.ScriptId ?? "(sin id)");
                else
                    AddRow("  " + c.GetType().Name, "");
            }
        }
    }

    private void BuildScriptVariables(PlayModeRunner runner, GameObject go)
    {
        ScriptVarsContent.Children.Clear();
        var rt = runner.GetRuntime();
        if (rt == null) return;

        var instances = rt.GetScriptInstancesFor(go);
        if (instances == null || instances.Count == 0)
        {
            var noScript = new TextBlock { Text = "Sin scripts en este objeto.", Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x8b, 0x94, 0x9e)), FontSize = 11, Margin = new Thickness(0, 4, 0, 0) };
            ScriptVarsContent.Children.Add(noScript);
            return;
        }

        var brushLabel = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x8b, 0x94, 0x9e));
        var brushVal = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xe6, 0xed, 0xf3));
        var brushScript = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x58, 0xa6, 0xff));

        foreach (var inst in instances)
        {
            var scriptHeader = new TextBlock { Text = $"Script: {inst.ScriptId}", Foreground = brushScript, FontWeight = FontWeights.SemiBold, FontSize = 11, Margin = new Thickness(0, 8, 0, 4) };
            ScriptVarsContent.Children.Add(scriptHeader);

            var snap = inst.GetVariableSnapshot();
            if (snap == null || snap.Count == 0)
            {
                var empty = new TextBlock { Text = "  (sin variables visibles)", Foreground = brushLabel, FontSize = 10, Margin = new Thickness(0, 0, 0, 4) };
                ScriptVarsContent.Children.Add(empty);
                continue;
            }
            foreach (var kv in snap)
            {
                var row = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(4, 1, 0, 2) };
                row.Children.Add(new TextBlock { Text = kv.Key + " = ", Foreground = brushLabel, FontSize = 11 });
                row.Children.Add(new TextBlock { Text = kv.Value ?? "nil", Foreground = brushVal, FontSize = 11, TextWrapping = TextWrapping.Wrap });
                ScriptVarsContent.Children.Add(row);
            }
        }
    }

    private void SceneObjectsList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        Refresh();
    }

    /// <summary>Llamar al cerrar el tab para detener el timer.</summary>
    public void StopRefresh()
    {
        _refreshTimer?.Stop();
        _refreshTimer = null;
    }
}
