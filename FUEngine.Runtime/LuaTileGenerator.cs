using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NLua;
using FUEngine.Runtime.Mathematics;

namespace FUEngine.Runtime;

/// <summary>
/// Runs a Lua script that defines onGenerateTile(canvas, width, height) and returns the generated tile as BGRA bytes.
/// Injects canvas, noise (Perlin), and optional property values. Supports property("Name", default, min, max) discovery.
/// </summary>
public static class LuaTileGenerator
{
    private static readonly PerlinNoise SharedNoise = new PerlinNoise(0);

    /// <summary>
    /// Discovers property("Name", default, min, max) calls in the script without running onGenerateTile.
    /// </summary>
    /// <returns>List of properties (order of first occurrence); error message if script fails.</returns>
    public static (IReadOnlyList<PropertyDefinition> properties, string? error) GetProperties(string scriptSource, string? scriptName = null)
    {
        var list = new List<PropertyDefinition>();
        Lua? state = null;
        try
        {
            state = new Lua();
            state.State.Encoding = Encoding.UTF8;

            state["property"] = (Action<string, double, double, double>)((name, defaultVal, min, max) =>
            {
                if (string.IsNullOrWhiteSpace(name)) return;
                list.Add(new PropertyDefinition
                {
                    Name = name.Trim(),
                    Default = defaultVal,
                    Min = min,
                    Max = max
                });
            });

            InjectNoise(state);
            InjectLerpClamp(state);
            state.DoString(scriptSource, scriptName ?? "tilegen");
            return (list, null);
        }
        catch (NLua.Exceptions.LuaException ex)
        {
            return (list, ex.Message ?? ex.ToString());
        }
        catch (Exception ex)
        {
            return (list, ex.Message ?? ex.ToString());
        }
        finally
        {
            state?.Dispose();
        }
    }

    private static void InjectNoise(Lua state)
    {
        state["noise"] = (Func<double, double, double>)((x, y) => SharedNoise.Noise(x, y));
        state["noise3"] = (Func<double, double, double, double>)((x, y, z) => SharedNoise.Noise(x, y, z));
        state.DoString("math.noise = function(x, y, z) if z then return noise3(x, y, z) else return noise(x, y) end end");
    }

    private static void InjectLerpClamp(Lua state)
    {
        state["lerp"] = (Func<double, double, double, double>)((a, b, t) => a + (b - a) * Math.Clamp(t, 0, 1));
        state["clamp"] = (Func<double, double, double, double>)((v, min, max) => Math.Clamp(v, min, max));
        state.DoString("math.lerp = lerp; math.clamp = clamp");
    }

    /// <summary>
    /// Runs the script from file and invokes onGenerateTile(canvas, width, height).
    /// </summary>
    public static (byte[]? bgra, int width, int height, string? error) Run(
        string scriptPath,
        int width,
        int height,
        Action<string>? printCallback = null,
        IReadOnlyDictionary<string, double>? propertyValues = null)
    {
        if (string.IsNullOrWhiteSpace(scriptPath) || !File.Exists(scriptPath))
            return (null, 0, 0, "Script file not found: " + (scriptPath ?? ""));
        if (width <= 0 || height <= 0)
            return (null, 0, 0, "Width and height must be positive.");

        string source;
        try
        {
            source = File.ReadAllText(scriptPath, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            return (null, 0, 0, "Could not read script: " + ex.Message);
        }

        return RunFromSource(source, width, height, scriptPath, printCallback, propertyValues);
    }

    /// <summary>
    /// Runs the script from source and invokes onGenerateTile(canvas, width, height).
    /// Injects noise(x,y) and optional property values as globals.
    /// </summary>
    public static (byte[]? bgra, int width, int height, string? error) RunFromSource(
        string scriptSource,
        int width,
        int height,
        string? scriptName = null,
        Action<string>? printCallback = null,
        IReadOnlyDictionary<string, double>? propertyValues = null)
    {
        if (width <= 0 || height <= 0)
            return (null, 0, 0, "Width and height must be positive.");

        Lua? state = null;
        try
        {
            state = new Lua();
            state.State.Encoding = Encoding.UTF8;

            var canvas = new TileGeneratorCanvas(width, height);
            state["canvas"] = canvas;

            state["property"] = (Action<string, double, double, double>)((_, __, ___, ____) => { });

            if (propertyValues != null)
            {
                foreach (var kv in propertyValues)
                    state[kv.Key] = kv.Value;
            }

            InjectNoise(state);
            InjectLerpClamp(state);

            if (printCallback != null)
                state["print"] = (Action<object[]>)(args =>
                {
                    var parts = (args ?? Array.Empty<object>()).Select(a => a?.ToString() ?? "");
                    printCallback(string.Join(" ", parts));
                });

            state.DoString(scriptSource, scriptName ?? "tilegen");

            var fn = state["onGenerateTile"] as LuaFunction;
            if (fn == null)
                return (null, 0, 0, "Script must define function onGenerateTile(canvas, width, height).");

            fn.Call(canvas, width, height);

            var buffer = canvas.GetBuffer();
            var result = new byte[buffer.Length];
            Array.Copy(buffer, result, result.Length);
            return (result, width, height, null);
        }
        catch (NLua.Exceptions.LuaException ex)
        {
            return (null, 0, 0, ex.Message ?? ex.ToString());
        }
        catch (Exception ex)
        {
            return (null, 0, 0, ex.Message ?? ex.ToString());
        }
        finally
        {
            state?.Dispose();
        }
    }
}
