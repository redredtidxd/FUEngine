using System.Linq;

namespace FUEngine;

/// <summary>Lista de referencia de atajos para ayuda (valores por defecto; el usuario puede cambiarlos en ajustes).</summary>
public static class EditorShortcutRegistry
{
    public sealed record Line(string Category, string Keys, string Description);

    public static IReadOnlyList<Line> Lines { get; } = EditorShortcutBindings.Definitions
        .Where(d => !d.Id.StartsWith('_'))
        .Select(d => new Line(d.Category, d.DefaultDisplay, d.Description))
        .ToList();
}
