namespace FUEngine.Spotlight;

/// <summary>Resultado unificado de FUEngine Spotlight (documentación, Lua, archivos, objetos, hub).</summary>
public sealed class SpotlightItem
{
    public SpotlightCategory Category { get; init; }
    public string Title { get; init; } = "";
    public string Subtitle { get; init; } = "";
    public string SearchText { get; init; } = "";

    public string? DocumentationTopicId { get; init; }
    public string? LuaSignature { get; init; }
    public string? LuaDetail { get; init; }
    public string? LuaExample { get; init; }
    public string? FilePath { get; init; }
    public string? ObjectInstanceId { get; init; }
    public string? HubProjectPath { get; init; }
    public string? ExternalMarkdownPath { get; init; }

    public string CategoryLabel => Category switch
    {
        SpotlightCategory.Documentation => "Documentación",
        SpotlightCategory.ScriptExamples => "Ejemplos Lua",
        SpotlightCategory.LuaApi => "Lua / API",
        SpotlightCategory.ProjectFile => "Proyecto",
        SpotlightCategory.SceneObject => "Escena",
        SpotlightCategory.HubProject => "Proyecto reciente",
        SpotlightCategory.ExternalDoc => "Archivo",
        _ => ""
    };

    /// <summary>Clave de agrupación en la UI (prefijo numérico fija el orden de secciones).</summary>
    public string GroupHeader => Category switch
    {
        SpotlightCategory.ExternalDoc => "01 — Archivos y notas (repo)",
        SpotlightCategory.Documentation => "02 — Documentación (manual integrado)",
        SpotlightCategory.ScriptExamples => "02b — Ejemplos de scripts (Lua)",
        SpotlightCategory.LuaApi => "03 — Lua (API motor + biblioteca estándar + hooks)",
        SpotlightCategory.HubProject => "04 — Proyectos recientes (Hub)",
        SpotlightCategory.ProjectFile => "05 — Archivos del proyecto (.lua / .map / .seed)",
        SpotlightCategory.SceneObject => "06 — Objetos en la escena",
        _ => "99 — Otros"
    };
}

public enum SpotlightCategory
{
    Documentation,
    LuaApi,
    ProjectFile,
    SceneObject,
    HubProject,
    ExternalDoc,
    /// <summary>Ejemplos de scripts (pestaña integrada).</summary>
    ScriptExamples
}
