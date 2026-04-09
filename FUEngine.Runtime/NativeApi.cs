// -----------------------------------------------------------------------------
// FUEngine (FUEngine.Runtime) — Condiciones: LICENSE.md en la raíz del repositorio.
// Copyright (c) Red Redtid. No es licencia MIT/Apache ni "open source" OSI.
// -----------------------------------------------------------------------------
namespace FUEngine.Runtime;

/// <summary>
/// Tabla global Lua <c>native</c>: delega en <see cref="NativeBridge"/> (C# es el único puente hacia la DLL).
/// </summary>
[LuaVisible]
public sealed class NativeApi
{
    /// <summary>True si <c>FUECoreNative.dll</c> se cargó correctamente.</summary>
    public bool isAvailable() => NativeBridge.IsLibraryLoaded;

    /// <summary>Ejemplo de interop por valor (enteros). Lua puede pasar números; se truncan a int.</summary>
    public double fastMathSum(double a, double b) =>
        NativeBridge.FastMathSum((int)a, (int)b);

    /// <summary>Cadena ANSI desde la nativa (<c>Marshal.PtrToStringAnsi</c>), o cadena vacía si no hay DLL/export.</summary>
    public string versionString() => NativeBridge.TryGetVersionStringAnsi() ?? "";
}
