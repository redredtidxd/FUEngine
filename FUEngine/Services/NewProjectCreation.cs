using System.IO;
using FUEngine.Core;
using FUEngine.Editor;

namespace FUEngine;

/// <summary>
/// Crea la estructura en disco a partir del asistente (<see cref="NewProjectWizardPanel"/>).
/// </summary>
public static class NewProjectCreation
{
    /// <returns>Ruta absoluta a <c>Project.FUE</c> creado.</returns>
    public static string CreateFromWizard(NewProjectWizardPanel wizard)
    {
        var projectDir = wizard.ProjectPath ?? "";
        if (!Directory.Exists(projectDir) && wizard.CreateProjectFolderIfMissing)
            Directory.CreateDirectory(projectDir);

        var engineSt = EngineSettings.Load();
        var roots = engineSt.GetResolvedNewProjectStandardRootFolders();
        var extras = engineSt.GetResolvedExtraNewProjectRootFolders();
        var project = new ProjectInfo
        {
            Nombre = wizard.ProjectName ?? "",
            Descripcion = wizard.Description ?? "",
            TileSize = wizard.TileSize,
            MapWidth = wizard.MapWidth,
            MapHeight = wizard.MapHeight,
            Infinite = false,
            ChunkSize = wizard.ChunkSize,
            InitialChunksW = wizard.InitialChunksW,
            InitialChunksH = wizard.InitialChunksH,
            ProjectDirectory = projectDir,
            AutoSaveEnabled = true,
            AutoSaveIntervalMinutes = 5,
            AutoSaveMaxBackupsPerType = 10,
            AutoSaveFolder = "Autoguardados",
            AutoSaveOnClose = true,
            PaletteId = string.IsNullOrWhiteSpace(engineSt.DefaultPaletteId) ? "default" : engineSt.DefaultPaletteId,
            TemplateType = null,
            Author = wizard.Author,
            Copyright = wizard.Copyright,
            Version = wizard.Version ?? "0.0.1",
            TileHeight = 1,
            AutoTiling = wizard.AutoTiling,
            Fps = wizard.Fps,
            PixelPerfect = wizard.PixelPerfect,
            InitialZoom = wizard.InitialZoom,
            LightShadowDefault = wizard.LightShadowDefault,
            DebugMode = wizard.DebugMode,
            ScriptNodes = wizard.ScriptNodes,
            DefaultFirstSceneBackgroundColor = wizard.DefaultFirstSceneBackgroundColor,
            ProjectFormatVersion = ProjectSchema.CurrentFormatVersion
        };
        var proyectoConfig = new ProyectoConfigDto
        {
            Nombre = project.Nombre,
            Logo = "logo.png",
            Plantilla = "Blank",
            AutoguardadoActivo = true,
            IntervaloAutoguardadoMin = 5,
            MaxBackupsAutoguardado = 10,
            GuardarSoloCambios = true,
            Descripcion = project.Descripcion,
            Autor = wizard.Author,
            Version = wizard.Version ?? "0.1"
        };
        return NewProjectStructure.Create(projectDir, project, wizard.IconPath, proyectoConfig, wizard.GenerateStandardHierarchy, roots, extras);
    }
}
