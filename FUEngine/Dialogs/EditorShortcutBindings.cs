using System.Linq;
using System.Windows.Input;

namespace FUEngine;

/// <summary>Definición y coincidencia de atajos del editor (persistencia en EngineSettings.shortcutBindings).</summary>
public static class EditorShortcutBindings
{
    public const string SaveMap = "SaveMap";
    public const string SaveAll = "SaveAll";
    public const string Undo = "Undo";
    public const string Redo = "Redo";
    public const string CopyZone = "CopyZone";
    public const string PasteZone = "PasteZone";
    public const string DeleteSelection = "DeleteSelection";
    public const string RotateObject = "RotateObject";
    public const string Tool1 = "Tool1";
    public const string Tool2 = "Tool2";
    public const string Tool3 = "Tool3";
    public const string Tool4 = "Tool4";
    public const string Tool5 = "Tool5";
    public const string Tool6 = "Tool6";
    public const string Play = "Play";
    public const string PausePlay = "PausePlay";
    public const string ToggleGrid = "ToggleGrid";
    public const string GroupObjects = "GroupObjects";
    public const string HandPan = "HandPan";
    public const string SelectAllMap = "SelectAllMap";

    public sealed record Definition(string Id, string Category, string Description, string DefaultDisplay, bool Rebindable);

    public static IReadOnlyList<Definition> Definitions { get; } = new[]
    {
        new Definition(SaveMap, "Archivo", "Guardar mapa", "Ctrl+S", true),
        new Definition(SaveAll, "Archivo", "Guardar todo", "Ctrl+Shift+S", true),
        new Definition(Undo, "Editar", "Deshacer", "Ctrl+Z", true),
        new Definition(Redo, "Editar", "Rehacer", "Ctrl+Y", true),
        new Definition(CopyZone, "Editar", "Copiar zona", "Ctrl+C", true),
        new Definition(PasteZone, "Editar", "Pegar zona", "Ctrl+V", true),
        new Definition(DeleteSelection, "Selección", "Borrar selección (tiles u objetos)", "Delete", true),
        new Definition(RotateObject, "Objetos", "Rotar objeto", "R", true),
        new Definition(Tool1, "Herramientas", "Herramienta 1 (Pincel)", "1", true),
        new Definition(Tool2, "Herramientas", "Herramienta 2 (Seleccionar)", "2", true),
        new Definition(Tool3, "Herramientas", "Herramienta 3 (Colocar)", "3", true),
        new Definition(Tool4, "Herramientas", "Herramienta 4 (Zona)", "4", true),
        new Definition(Tool5, "Herramientas", "Herramienta 5 (Medir)", "5", true),
        new Definition(Tool6, "Herramientas", "Herramienta 6 (Pixel)", "6", true),
        new Definition(Play, "Juego", "Ejecutar proyecto (play)", "F5", true),
        new Definition(PausePlay, "Juego", "Pausar / reanudar play", "F6", true),
        new Definition(ToggleGrid, "Vista", "Mostrar u ocultar grid", "G", true),
        new Definition(GroupObjects, "Objetos", "Agrupar objetos (experimental)", "Ctrl+G", true),
        new Definition(HandPan, "Vista", "Mano / pan (mantener y arrastrar con clic izq.)", "Space", true),
        new Definition(SelectAllMap, "Mapa", "Seleccionar todo en la capa activa (tiles + objetos con mismo orden)", "Ctrl+A", true),
        new Definition("_ReadOnlyPanKeys", "Vista", "Desplazar vista con teclado (mapa con foco)", "W A S D / flechas", false),
    };

    public static string GetDisplay(EngineSettings settings, string id)
    {
        var map = settings.ShortcutBindings;
        if (map != null && map.TryGetValue(id, out var v) && !string.IsNullOrWhiteSpace(v))
            return v.Trim();
        var def = Definitions.FirstOrDefault(d => d.Id == id);
        return def?.DefaultDisplay ?? "";
    }

    public static bool MatchesDisplay(string? bindingDisplay, Key key, ModifierKeys mods)
    {
        if (string.IsNullOrWhiteSpace(bindingDisplay)) return false;
        return TryParse(bindingDisplay.Trim(), out var k, out var m) && k == key && NormalizeMods(m) == NormalizeMods(mods);
    }

    private static ModifierKeys NormalizeMods(ModifierKeys m) =>
        m & (ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt);

    public static bool TryParse(string s, out Key key, out ModifierKeys mods)
    {
        key = Key.None;
        mods = ModifierKeys.None;
        if (string.IsNullOrWhiteSpace(s)) return false;
        var parts = s.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return false;
        for (var i = 0; i < parts.Length - 1; i++)
        {
            var p = parts[i];
            if (p.Equals("Ctrl", StringComparison.OrdinalIgnoreCase)) mods |= ModifierKeys.Control;
            else if (p.Equals("Shift", StringComparison.OrdinalIgnoreCase)) mods |= ModifierKeys.Shift;
            else if (p.Equals("Alt", StringComparison.OrdinalIgnoreCase)) mods |= ModifierKeys.Alt;
        }
        var last = parts[^1];
        key = KeyFromToken(last);
        return key != Key.None;
    }

    private static Key KeyFromToken(string token)
    {
        if (string.IsNullOrEmpty(token)) return Key.None;
        if (token.Equals("Space", StringComparison.OrdinalIgnoreCase)) return Key.Space;
        if (token.Equals("Delete", StringComparison.OrdinalIgnoreCase)) return Key.Delete;
        if (token.Equals("Return", StringComparison.OrdinalIgnoreCase) || token.Equals("Enter", StringComparison.OrdinalIgnoreCase)) return Key.Enter;
        if (token.Length == 1)
        {
            var c = char.ToUpperInvariant(token[0]);
            if (c >= 'A' && c <= 'Z') return Key.A + (c - 'A');
            if (c >= '0' && c <= '9') return Key.D0 + (c - '0');
        }
        if (token.StartsWith("F", StringComparison.OrdinalIgnoreCase) && int.TryParse(token.AsSpan(1), out var fn) && fn is >= 1 and <= 24)
            return Key.F1 + fn - 1;
        if (Enum.TryParse<Key>(token, true, out var ek)) return ek;
        return Key.None;
    }

    public static string FormatKeyGesture(Key key, ModifierKeys mods)
    {
        var parts = new List<string>();
        if ((mods & ModifierKeys.Control) != 0) parts.Add("Ctrl");
        if ((mods & ModifierKeys.Shift) != 0) parts.Add("Shift");
        if ((mods & ModifierKeys.Alt) != 0) parts.Add("Alt");
        parts.Add(KeyToToken(key));
        return string.Join("+", parts);
    }

    private static string KeyToToken(Key key) => key switch
    {
        Key.Space => "Space",
        Key.Delete => "Delete",
        Key.Enter => "Enter",
        >= Key.D0 and <= Key.D9 => ((char)('0' + (key - Key.D0))).ToString(),
        >= Key.NumPad0 and <= Key.NumPad9 => "NumPad" + (key - Key.NumPad0),
        >= Key.A and <= Key.Z => ((char)('A' + (key - Key.A))).ToString(),
        >= Key.F1 and <= Key.F24 => "F" + (1 + (key - Key.F1)),
        _ => key.ToString()
    };

    public static string? MatchActionId(EngineSettings settings, Key key, ModifierKeys mods)
    {
        foreach (var d in Definitions)
        {
            if (!d.Rebindable || d.Id.StartsWith('_')) continue;
            var disp = GetDisplay(settings, d.Id);
            if (MatchesDisplay(disp, key, mods))
                return d.Id;
        }
        return null;
    }
}
