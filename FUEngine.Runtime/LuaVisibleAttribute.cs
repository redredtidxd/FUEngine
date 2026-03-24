namespace FUEngine.Runtime;

/// <summary>Marcador para reflexión del mini-IDE: miembros expuestos a Lua (catálogo de autocompletado).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property)]
public sealed class LuaVisibleAttribute : Attribute
{
}
