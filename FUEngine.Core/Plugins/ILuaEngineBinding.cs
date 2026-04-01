namespace FUEngine.Core;

/// <summary>
/// Plugin opcional que registra tablas o funciones en el intérprete Lua del motor.
/// El host Lua se pasa como <see cref="object"/> para no referenciar NLua desde Core.
/// </summary>
public interface ILuaEngineBinding
{
    /// <summary>Registra bindings en el host Lua usando el contexto del motor (world, config, APIs, servicios).</summary>
    void RegisterLuaHost(object luaState, IEngineContext context);
}
