using System.Collections.Generic;

namespace FUEngine;

/// <summary>Enlaces entre el Inspector de scripts y el catálogo de APIs (mismas globales que el autocompletado).</summary>
public static class LuaScriptVariableDiscovery
{
    public static IReadOnlyList<string> MotorGlobalNames => LuaEditorCompletionCatalog.Globals;
}
