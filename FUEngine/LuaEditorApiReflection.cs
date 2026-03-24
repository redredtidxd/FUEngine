using System.Collections.Generic;
using System.Reflection;
using FUEngine.Runtime;

namespace FUEngine;

/// <summary>Rellena el mapa prefijo → miembros Lua desde tipos marcados con <see cref="LuaVisibleAttribute"/>.</summary>
internal static class LuaEditorApiReflection
{
    public static Dictionary<string, string[]> BuildMemberMap()
    {
        var map = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        Add(map, "world.", typeof(WorldApi));
        Add(map, "self.", typeof(SelfProxy));
        Add(map, "layer.", typeof(LayerProxy));
        Add(map, "input.", typeof(InputApi));
        Add(map, "time.", typeof(TimeApi));
        Add(map, "audio.", typeof(AudioApi));
        Add(map, "physics.", typeof(PhysicsApi));
        Add(map, "ui.", typeof(UiApi));
        Add(map, "game.", typeof(GameApi));
        Add(map, "ads.", typeof(AdsApi));
        Add(map, "Debug.", typeof(DebugDrawApi));
        Add(map, "Key.", typeof(KeyConstants));
        Add(map, "Mouse.", typeof(MouseConstants));
        Add(map, "component.", typeof(ComponentProxy));
        return map;
    }

    private static void Add(Dictionary<string, string[]> map, string prefix, Type type)
    {
        if (!type.IsDefined(typeof(LuaVisibleAttribute), false))
            return;
        var set = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        const BindingFlags inst = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        for (var t = type; t != null && t != typeof(object); t = t.BaseType)
        {
            foreach (var m in t.GetMethods(inst))
            {
                if (m.IsSpecialName) continue;
                set.Add(m.Name);
            }
            foreach (var p in t.GetProperties(inst))
            {
                if (p.GetIndexParameters().Length > 0) continue;
                set.Add(p.Name);
            }
            foreach (var f in t.GetFields(inst))
                set.Add(f.Name);
        }
        map[prefix] = set.ToArray();
    }
}
