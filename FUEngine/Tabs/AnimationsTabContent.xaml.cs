using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using FUEngine.Core;

namespace FUEngine;

public partial class AnimationsTabContent : System.Windows.Controls.UserControl
{
    public event EventHandler<AnimationDefinition?>? AnimationSelected;

    private DispatcherTimer? _previewTimer;
    private int _previewFrameIndex;

    public AnimationsTabContent()
    {
        InitializeComponent();
        Unloaded += (_, _) => StopPreviewTimer();
    }

    public void SetAnimations(IEnumerable<AnimationDefinition>? animations)
    {
        AnimationsList.Items.Clear();
        if (animations == null) return;
        foreach (var a in animations)
            AnimationsList.Items.Add(a);
        AnimationsList.DisplayMemberPath = nameof(AnimationDefinition.Nombre);
    }

    private void BtnCreateAnimation_OnClick(object sender, RoutedEventArgs e)
    {
        System.Windows.MessageBox.Show("Crear animación: diálogo en desarrollo. Añade animaciones en animaciones.json.", "Animaciones", MessageBoxButton.OK);
    }

    private void AnimationsList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var anim = AnimationsList.SelectedItem as AnimationDefinition;
        StopPreviewTimer();
        if (anim == null)
        {
            TxtAnimationPreview.Text = "Selecciona una animación para ver preview y propiedades en el Inspector.";
            PreviewArea.Visibility = Visibility.Collapsed;
            FramesStrip.ItemsSource = null;
            AnimationSelected?.Invoke(this, null);
            return;
        }
        TxtAnimationPreview.Text = $"{anim.Nombre} · Propiedades en el Inspector.";
        PreviewArea.Visibility = Visibility.Visible;
        var frameLabels = anim.Frames.Count > 0
            ? anim.Frames.Select((f, i) => string.IsNullOrEmpty(f) ? $"{i}" : System.IO.Path.GetFileName(f)).ToList()
            : new List<string> { "0" };
        FramesStrip.ItemsSource = frameLabels;
        TxtPreviewFps.Text = $"{anim.Fps} FPS";
        _previewFrameIndex = 0;
        UpdatePreviewFrame(anim);
        var fps = Math.Max(1, anim.Fps);
        _previewTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(1000.0 / fps)
        };
        _previewTimer.Tick += (_, _) =>
        {
            if (AnimationsList.SelectedItem is not AnimationDefinition current) return;
            _previewFrameIndex = (_previewFrameIndex + 1) % Math.Max(1, current.Frames.Count);
            UpdatePreviewFrame(current);
        };
        _previewTimer.Start();
        AnimationSelected?.Invoke(this, anim);
    }

    private void UpdatePreviewFrame(AnimationDefinition anim)
    {
        var count = Math.Max(1, anim.Frames.Count);
        TxtPreviewFrame.Text = anim.Frames.Count == 0
            ? "Sin frames"
            : $"Frame {_previewFrameIndex + 1} / {count}";
    }

    private void StopPreviewTimer()
    {
        _previewTimer?.Stop();
        _previewTimer = null;
    }
}
