using System;

namespace FUEngine.Runtime;

/// <summary>API <c>log.info</c> / <c>log.warn</c> / <c>log.error</c> inyectada en scripts Lua.</summary>
[LuaVisible]
public sealed class LuaLogApi
{
    private readonly Action<string, string> _sink;

    public LuaLogApi(Action<string, string> sink)
    {
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
    }

    public void info(string? msg) => _sink("info", msg ?? "");

    public void warn(string? msg) => _sink("warn", msg ?? "");

    public void error(string? msg) => _sink("error", msg ?? "");
}
