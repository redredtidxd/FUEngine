using FUEngine.Help;

namespace FUEngine;

/// <summary>Nivel de dificultad en la pestaña «Ejemplos de scripts» (punto de color en la lista).</summary>
public enum ScriptExampleDifficultyTier
{
    None = 0,
    Basic = 1,
    Intermediate = 2,
    Advanced = 3
}

/// <summary>Elemento de la lista lateral de ayuda: tema + categoría para agrupar.</summary>
public sealed class DocumentationTopicListEntry
{
    public DocumentationTopicListEntry(
        int groupOrder,
        string groupTitle,
        string displayLabel,
        DocumentationTopic topic,
        ScriptExampleDifficultyTier difficultyTier = ScriptExampleDifficultyTier.None)
    {
        GroupOrder = groupOrder;
        GroupTitle = groupTitle;
        DisplayLabel = displayLabel;
        Topic = topic;
        DifficultyTier = difficultyTier;
    }

    /// <summary>Orden de la sección (menor = más arriba).</summary>
    public int GroupOrder { get; }

    /// <summary>Título de grupo mostrado en la lista (cabecera de categoría).</summary>
    public string GroupTitle { get; }

    /// <summary>Texto de la fila (título + prefijos opcionales).</summary>
    public string DisplayLabel { get; }

    public DocumentationTopic Topic { get; }

    /// <summary>Solo en ejemplos: punto de color verde / ámbar / rojo junto al título.</summary>
    public ScriptExampleDifficultyTier DifficultyTier { get; }
}
