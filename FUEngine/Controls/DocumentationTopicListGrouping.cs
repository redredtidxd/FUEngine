using System;
using System.Collections.Generic;
using FUEngine.Help;

namespace FUEngine;

/// <summary>Calcula categoría y etiqueta para la lista agrupada de ayuda.</summary>
internal static class DocumentationTopicListGrouping
{
    private static readonly Dictionary<string, int> ManualGroupOrder = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Inicio"] = 0,
        ["Motor / repo"] = 5,
        ["Pestañas"] = 8,
        ["Editor"] = 10,
        ["Componentes"] = 18,
        ["Mapa"] = 20,
        ["Objetos / Inspector"] = 25,
        ["Triggers"] = 30,
        ["Seeds"] = 35,
        ["Scripts (editor)"] = 38,
        ["Lua (runtime)"] = 40,
        ["Play / runtime"] = 45,
        ["UI"] = 50,
        ["Audio / render"] = 55,
        ["Proyecto / datos"] = 60,
        ["Exportación"] = 65,
        ["Depuración"] = 70,
        ["Creative Suite"] = 75,
        ["General"] = 100
    };

    private static readonly Dictionary<string, int> ScriptCategoryOrder = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Introducción"] = 0,
        ["Gameplay"] = 10,
        ["Editor"] = 15,
        ["Escenas"] = 20,
        ["UI"] = 25,
        ["Mapa"] = 30,
        ["Efectos"] = 35,
        ["Arquitectura"] = 40,
        ["Motor"] = 45,
        ["Datos"] = 50,
        ["General"] = 99
    };

    public static DocumentationTopicListEntry Create(
        DocumentationTopic topic,
        bool luaReferenceMode,
        bool scriptExamplesMode,
        IReadOnlyList<DocumentationTopic> visiblePeers)
    {
        int order;
        string groupTitle;

        if (scriptExamplesMode)
        {
            if (string.Equals(topic.Id, EngineDocumentation.ScriptExamplesIntroTopicId, StringComparison.Ordinal))
            {
                order = 0;
                groupTitle = "Introducción";
            }
            else
            {
                groupTitle = string.IsNullOrWhiteSpace(topic.ExampleCategory) ? "General" : topic.ExampleCategory.Trim();
                order = ScriptCategoryOrder.GetValueOrDefault(groupTitle, 50);
            }
        }
        else if (luaReferenceMode)
        {
            if (string.Equals(topic.Id, EngineDocumentation.LuaReferenceIntroTopicId, StringComparison.Ordinal))
            {
                order = 0;
                groupTitle = "Introducción";
            }
            else if (topic.Id.StartsWith("lua-kw-", StringComparison.Ordinal))
            {
                order = 20;
                groupTitle = "Palabras reservadas";
            }
            else if (topic.Id.StartsWith("lua-guide-", StringComparison.Ordinal))
            {
                order = 30;
                groupTitle = "Guías";
            }
            else
            {
                order = 40;
                groupTitle = "Lua / API y referencia";
            }
        }
        else
        {
            groupTitle = ExtractManualGroupTitle(topic);
            order = ManualGroupOrder.GetValueOrDefault(groupTitle, 100);
        }

        var label = FormatDisplayLabel(topic, scriptExamplesMode, visiblePeers);
        return new DocumentationTopicListEntry(order, groupTitle, label, topic);
    }

    private static string ExtractManualGroupTitle(DocumentationTopic topic)
    {
        var sub = topic.Subtitle?.Trim();
        if (string.IsNullOrEmpty(sub)) return "General";
        var idx = sub.IndexOf(" · ", StringComparison.Ordinal);
        if (idx > 0) return sub[..idx].Trim();
        return sub;
    }

    private static string FormatDisplayLabel(DocumentationTopic topic, bool scriptExamplesMode, IReadOnlyList<DocumentationTopic> visiblePeers)
    {
        var title = topic.Title ?? "";
        if (scriptExamplesMode && !string.IsNullOrEmpty(topic.ExampleDifficulty))
        {
            var badge = topic.ExampleDifficulty switch
            {
                "Básico" => "🟢 ",
                "Intermedio" => "🟡 ",
                "Avanzado" => "🔴 ",
                _ => ""
            };
            title = badge + title;
        }

        if (scriptExamplesMode && !string.IsNullOrEmpty(topic.ExampleCategory))
            title = topic.ExampleCategory + " · " + title;

        var sameTitle = false;
        foreach (var x in visiblePeers)
        {
            if (ReferenceEquals(x, topic)) continue;
            if (string.Equals(x.Title ?? "", topic.Title ?? "", StringComparison.Ordinal)
                && string.Equals(x.ExampleCategory ?? "", topic.ExampleCategory ?? "", StringComparison.Ordinal))
            {
                sameTitle = true;
                break;
            }
        }

        return sameTitle ? $"{title}  ({topic.Id})" : title;
    }
}
