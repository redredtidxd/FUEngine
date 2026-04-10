using System.Windows;
using System.Windows.Controls;
using FUEngine.Core;

namespace FUEngine;

public partial class ProjectManifestPanel : System.Windows.Controls.UserControl
{
    private ProjectInfo? _project;

    public event EventHandler? RequestSaveAfterApply;
    public event EventHandler? RequestExportBuild;
    public event EventHandler? RequestIntegrityCheck;
    public event EventHandler? RequestOpenProjectFolder;
    public event EventHandler? RequestAdvancedConfig;

    public ProjectManifestPanel()
    {
        InitializeComponent();
    }

    public void LoadFromProject(ProjectInfo project, string? manifestPath)
    {
        _project = project;
        if (TxtManifestHint != null)
        {
            TxtManifestHint.Text = string.IsNullOrEmpty(manifestPath)
                ? ""
                : $"Archivo: {manifestPath}";
        }
        bool autoRes = project.GameResolutionWidth <= 0 && project.GameResolutionHeight <= 0;
        if (ChkGameResolutionAuto != null) ChkGameResolutionAuto.IsChecked = autoRes;
        if (autoRes)
        {
            TxtGameW.Text = "0";
            TxtGameH.Text = "0";
        }
        else
        {
            TxtGameW.Text = project.GameResolutionWidth.ToString();
            TxtGameH.Text = project.GameResolutionHeight.ToString();
        }
        ApplyGameResolutionFieldsEnabledState();
        TxtTileSize.Text = project.TileSize.ToString();
        TxtNombre.Text = project.Nombre ?? "";
        TxtVersion.Text = project.Version ?? "";
        TxtAuthor.Text = project.Author ?? "";
        TxtCopyright.Text = project.Copyright ?? "";
        ChkUseNativeCameraFollow.IsChecked = project.UseNativeCameraFollow;
        TxtDefaultSceneBg.Text = project.DefaultFirstSceneBackgroundColor ?? "#FFFFFF";
        TxtEditorCanvasBg.Text = project.EditorMapCanvasBackgroundColor ?? "#21262d";
    }

    private void ApplyGameResolutionFieldsEnabledState()
    {
        bool auto = ChkGameResolutionAuto?.IsChecked == true;
        if (TxtGameW != null) TxtGameW.IsEnabled = !auto;
        if (TxtGameH != null) TxtGameH.IsEnabled = !auto;
    }

    private void ChkGameResolutionAuto_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_project == null || ChkGameResolutionAuto == null) return;
        ApplyGameResolutionFieldsEnabledState();
        if (ChkGameResolutionAuto.IsChecked == true)
        {
            TxtGameW.Text = "0";
            TxtGameH.Text = "0";
        }
        else
        {
            if (_project.GameResolutionWidth > 0 && _project.GameResolutionHeight > 0)
            {
                TxtGameW.Text = _project.GameResolutionWidth.ToString();
                TxtGameH.Text = _project.GameResolutionHeight.ToString();
            }
            else
            {
                TxtGameW.Text = "1280";
                TxtGameH.Text = "720";
            }
        }
    }

    public bool TryApplyToProject(ProjectInfo p)
    {
        if (ChkGameResolutionAuto?.IsChecked == true)
        {
            p.GameResolutionWidth = 0;
            p.GameResolutionHeight = 0;
        }
        else
        {
            if (!int.TryParse(TxtGameW.Text?.Trim(), out var w) || w <= 0)
            {
                EditorLog.Toast("Ancho de resolución inválido (debe ser un entero mayor que 0).", LogLevel.Warning, "Proyecto");
                return false;
            }
            if (!int.TryParse(TxtGameH.Text?.Trim(), out var h) || h <= 0)
            {
                EditorLog.Toast("Alto de resolución inválido (debe ser un entero mayor que 0).", LogLevel.Warning, "Proyecto");
                return false;
            }
            p.GameResolutionWidth = w;
            p.GameResolutionHeight = h;
        }
        if (!int.TryParse(TxtTileSize.Text?.Trim(), out var ts) || ts <= 0)
        {
            EditorLog.Toast("Tile size inválido (debe ser un entero mayor que 0).", LogLevel.Warning, "Proyecto");
            return false;
        }
        p.TileSize = ts;
        p.Nombre = (TxtNombre.Text ?? "").Trim();
        p.Version = string.IsNullOrWhiteSpace(TxtVersion.Text) ? "0.0.1" : TxtVersion.Text.Trim();
        p.Author = string.IsNullOrWhiteSpace(TxtAuthor.Text) ? null : TxtAuthor.Text.Trim();
        p.Copyright = string.IsNullOrWhiteSpace(TxtCopyright.Text) ? null : TxtCopyright.Text.Trim();
        p.UseNativeCameraFollow = ChkUseNativeCameraFollow.IsChecked == true;
        p.DefaultFirstSceneBackgroundColor = string.IsNullOrWhiteSpace(TxtDefaultSceneBg.Text) ? "#FFFFFF" : TxtDefaultSceneBg.Text.Trim();
        p.EditorMapCanvasBackgroundColor = string.IsNullOrWhiteSpace(TxtEditorCanvasBg.Text) ? "#21262d" : TxtEditorCanvasBg.Text.Trim();
        return true;
    }

    private void BtnGuardar_OnClick(object sender, RoutedEventArgs e)
    {
        if (_project == null) return;
        RequestSaveAfterApply?.Invoke(this, EventArgs.Empty);
    }

    private void BtnExport_OnClick(object sender, RoutedEventArgs e) => RequestExportBuild?.Invoke(this, EventArgs.Empty);

    private void BtnIntegrity_OnClick(object sender, RoutedEventArgs e) => RequestIntegrityCheck?.Invoke(this, EventArgs.Empty);

    private void BtnOpenFolder_OnClick(object sender, RoutedEventArgs e) => RequestOpenProjectFolder?.Invoke(this, EventArgs.Empty);

    private void BtnAdvanced_OnClick(object sender, RoutedEventArgs e) => RequestAdvancedConfig?.Invoke(this, EventArgs.Empty);
}
