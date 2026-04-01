using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using FUEngine.Core;
using FUEngine.Editor;

namespace FUEngine;

public partial class SimulateWindow : Window
{
    private readonly ProjectInfo _project;
    private readonly TileMap _tileMap;
    private readonly ObjectLayer _objectLayer;
    private List<TriggerZone> _zones = new();
    private double _playerX = 2, _playerY = 2;
    private readonly HashSet<string> _enteredZones = new();
    private readonly GameTiming _timing = new() { TargetFps = 60 };

    public SimulateWindow(ProjectInfo project, TileMap tileMap, ObjectLayer objectLayer)
    {
        _project = project;
        _tileMap = tileMap;
        _objectLayer = objectLayer;
        InitializeComponent();
        LoadZones();
        DrawSimulation();
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        timer.Tick += (_, _) =>
        {
            _timing.Tick();
            if (TxtFps != null) TxtFps.Text = $"FPS: {_project.Fps} | Δt: {_timing.DeltaTime:F3}s";
        };
        timer.Start();
    }

    private void LoadZones()
    {
        try
        {
            if (File.Exists(_project.TriggerZonesPath))
                _zones = TriggerZoneSerialization.Load(_project.TriggerZonesPath);
        }
        catch { /* ignore */ }
    }

    private void DrawSimulation()
    {
        SimCanvas.Children.Clear();
        int tileSize = Math.Max(8, _project.TileSize);
        var brushByType = new Dictionary<TileType, System.Windows.Media.Brush>
        {
            [TileType.Suelo] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(80, 80, 80)),
            [TileType.Pared] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(120, 80, 60)),
            [TileType.Objeto] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(90, 90, 120)),
            [TileType.Especial] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 60, 100))
        };
        foreach (var (cx, cy) in _tileMap.EnumerateChunkCoords())
        {
            var chunk = _tileMap.GetChunk(cx, cy);
            if (chunk == null) continue;
            foreach (var (lx, ly, data) in chunk.EnumerateTiles())
            {
                int wx = cx * _tileMap.ChunkSize + lx;
                int wy = cy * _tileMap.ChunkSize + ly;
                var rect = new System.Windows.Shapes.Rectangle
                {
                    Width = tileSize,
                    Height = tileSize,
                    Fill = brushByType.GetValueOrDefault(data.TipoTile, System.Windows.Media.Brushes.Gray)
                };
                System.Windows.Controls.Canvas.SetLeft(rect, wx * tileSize);
                System.Windows.Controls.Canvas.SetTop(rect, wy * tileSize);
                SimCanvas.Children.Add(rect);
            }
        }
        foreach (var z in _zones)
        {
            var zoneRect = new System.Windows.Shapes.Rectangle
            {
                Width = z.Width * tileSize,
                Height = z.Height * tileSize,
                Stroke = new SolidColorBrush(System.Windows.Media.Color.FromArgb(120, 0x58, 0xa6, 0xff)),
                Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(30, 0x58, 0xa6, 0xff))
            };
            System.Windows.Controls.Canvas.SetLeft(zoneRect, z.X * tileSize);
            System.Windows.Controls.Canvas.SetTop(zoneRect, z.Y * tileSize);
            SimCanvas.Children.Add(zoneRect);
        }
        var player = new Ellipse
        {
            Width = tileSize * 0.7,
            Height = tileSize * 0.7,
            Fill = System.Windows.Media.Brushes.Lime
        };
        System.Windows.Controls.Canvas.SetLeft(player, _playerX * tileSize);
        System.Windows.Controls.Canvas.SetTop(player, _playerY * tileSize);
        SimCanvas.Children.Add(player);
        CheckZones();
    }

    private void CheckZones()
    {
        int px = (int)Math.Floor(_playerX), py = (int)Math.Floor(_playerY);
        foreach (var z in _zones)
        {
            if (!z.Contains(px, py)) continue;
            var key = z.Id;
            if (_enteredZones.Add(key))
            {
                TxtLog.Text += $"[Entrada] Zona: {z.Nombre}\n";
                if (!string.IsNullOrEmpty(z.ScriptIdOnEnter))
                    TxtLog.Text += $"  → Script: {z.ScriptIdOnEnter}\n";
                EditorLog.Info($"[Simulación] Zona '{z.Nombre}' (script: {z.ScriptIdOnEnter ?? "-"})", "Simulación");
            }
        }
        _enteredZones.RemoveWhere(id => _zones.All(z => z.Id != id || !z.Contains(px, py)));
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        double step = 0.25;
        bool moved = false;
        if (e.Key == System.Windows.Input.Key.Left) { _playerX = Math.Max(0, _playerX - step); moved = true; }
        if (e.Key == Key.Right) { _playerX += step; moved = true; }
        if (e.Key == System.Windows.Input.Key.Up) { _playerY = Math.Max(0, _playerY - step); moved = true; }
        if (e.Key == System.Windows.Input.Key.Down) { _playerY += step; moved = true; }
        if (moved)
        {
            int tx = (int)Math.Floor(_playerX), ty = (int)Math.Floor(_playerY);
            if (_tileMap.IsCollisionAt(tx, ty))
            {
                _playerX = Math.Floor(_playerX) + 0.5;
                _playerY = Math.Floor(_playerY) + 0.5;
            }
            DrawSimulation();
        }
    }
}
