using FUEngine.Help;

namespace FUEngine;

/// <summary>Elemento de la lista lateral de ayuda: tema + categoría para agrupar.</summary>
public sealed class DocumentationTopicListEntry
{
    public DocumentationTopicListEntry(int groupOrder, string groupTitle, string displayLabel, DocumentationTopic topic)
    {
        GroupOrder = groupOrder;
        GroupTitle = groupTitle;
        DisplayLabel = displayLabel;
        Topic = topic;
    }

    /// <summary>Orden de la sección (menor = más arriba).</summary>
    public int GroupOrder { get; }

    /// <summary>Título de grupo mostrado en la lista (cabecera de categoría).</summary>
    public string GroupTitle { get; }

    /// <summary>Texto de la fila (título + prefijos opcionales).</summary>
    public string DisplayLabel { get; }

    public DocumentationTopic Topic { get; }
}
