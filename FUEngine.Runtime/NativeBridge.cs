// -----------------------------------------------------------------------------
// FUEngine (FUEngine.Runtime) — Condiciones: LICENSE.md en la raíz del repositorio.
// Copyright (c) Red Redtid. No es licencia MIT/Apache ni "open source" OSI.
// -----------------------------------------------------------------------------
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace FUEngine.Runtime;

/// <summary>
/// Puente P/Invoke hacia <c>FUECoreNative.dll</c> (u homónimo sin extensión). Carga diferida;
/// si la biblioteca no existe, las llamadas devuelven valores seguros y se registra un aviso (una vez).
/// Lua no debe usar <c>DllImport</c>: solo esta capa C# y la tabla <c>native</c> en NLua.
/// </summary>
public static class NativeBridge
{
    /// <summary>Severidad para el sink de diagnóstico (equivalente aproximado a <c>IEditorLog</c> en el host).</summary>
    public enum DiagnosticSeverity
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// Registrado por el host (p. ej. <c>PlayModeRunner</c> → <c>EditorLog</c>). Si es null, se usa <see cref="Debug.WriteLine"/>.
    /// </summary>
    public static Action<DiagnosticSeverity, string, Exception?>? DiagnosticSink { get; set; }

    private const string LibraryFileName = "FUECoreNative.dll";
    private const string LibraryName = "FUECoreNative";

    private static readonly object InitLock = new();
    private static bool _initDone;
    private static bool _libraryLoaded;
    private static IntPtr _libraryHandle;
    private static bool _missingDllLogged;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int FuFastMathSumFn(int a, int b);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr FuVersionStringFn();

    private static FuFastMathSumFn? _fastMathSum;
    private static FuVersionStringFn? _versionString;

    /// <summary>True si se cargó la DLL nativa (aunque algún export concreto pueda faltar).</summary>
    public static bool IsLibraryLoaded
    {
        get
        {
            EnsureInitialized();
            return _libraryLoaded;
        }
    }

    /// <summary>
    /// Suma entera expuesta como ejemplo de llamada por valor. Requiere export C <c>fu_fast_math_sum</c>.
    /// Si la DLL no está o falla, devuelve 0.
    /// </summary>
    public static int FastMathSum(int a, int b)
    {
        EnsureInitialized();
        var fn = _fastMathSum;
        if (fn == null)
            return 0;
        try
        {
            return fn(a, b);
        }
        catch (Exception ex)
        {
            Emit(DiagnosticSeverity.Warning, "native: fu_fast_math_sum falló.", ex);
            return 0;
        }
    }

    /// <summary>
    /// Lee una cadena ANSI devuelta por la nativa (<c>char*</c>). Requiere export <c>fu_version_string</c>.
    /// Usa <see cref="Marshal.PtrToStringAnsi"/>; el llamador no debe liberar el puntero salvo que el contrato nativo lo exija.
    /// </summary>
    public static string? TryGetVersionStringAnsi()
    {
        EnsureInitialized();
        var fn = _versionString;
        if (fn == null)
            return null;
        try
        {
            var ptr = fn();
            if (ptr == IntPtr.Zero)
                return null;
            return Marshal.PtrToStringAnsi(ptr);
        }
        catch (Exception ex)
        {
            Emit(DiagnosticSeverity.Warning, "native: fu_version_string falló.", ex);
            return null;
        }
    }

    private static void EnsureInitialized()
    {
        if (_initDone)
            return;
        lock (InitLock)
        {
            if (_initDone)
                return;
            TryLoadNativeLibrary();
            _initDone = true;
        }
    }

    private static void TryLoadNativeLibrary()
    {
        _libraryLoaded = false;
        _fastMathSum = null;
        _versionString = null;

        var baseDir = AppContext.BaseDirectory;
        var candidates = new List<string>(4);
        if (!string.IsNullOrEmpty(baseDir))
        {
            candidates.Add(Path.Combine(baseDir, LibraryFileName));
            candidates.Add(Path.Combine(baseDir, LibraryName));
        }

        candidates.Add(LibraryFileName);
        candidates.Add(LibraryName);

        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                if (NativeLibrary.TryLoad(candidate, out var handle))
                {
                    _libraryHandle = handle;
                    _libraryLoaded = true;
                    BindExports(handle);
                    return;
                }
            }
            catch
            {
                /* siguiente candidato */
            }
        }

        LogMissingDllOnce();
    }

    private static void BindExports(IntPtr handle)
    {
        if (NativeLibrary.TryGetExport(handle, "fu_fast_math_sum", out var pSum))
        {
            try
            {
                _fastMathSum = Marshal.GetDelegateForFunctionPointer<FuFastMathSumFn>(pSum);
            }
            catch (Exception ex)
            {
                Emit(DiagnosticSeverity.Warning, "native: no se pudo enlazar fu_fast_math_sum.", ex);
            }
        }

        if (NativeLibrary.TryGetExport(handle, "fu_version_string", out var pVer))
        {
            try
            {
                _versionString = Marshal.GetDelegateForFunctionPointer<FuVersionStringFn>(pVer);
            }
            catch (Exception ex)
            {
                Emit(DiagnosticSeverity.Warning, "native: no se pudo enlazar fu_version_string.", ex);
            }
        }
    }

    private static void LogMissingDllOnce()
    {
        if (_missingDllLogged)
            return;
        _missingDllLogged = true;
        Emit(DiagnosticSeverity.Warning,
            $"No se encontró la biblioteca nativa «{LibraryFileName}» (búsqueda junto al ejecutable y por nombre). " +
            "La tabla Lua «native» seguirá disponible con valores por defecto (degradación elegante).",
            null);
    }

    private static void Emit(DiagnosticSeverity severity, string message, Exception? ex)
    {
        if (DiagnosticSink != null)
            DiagnosticSink(severity, message, ex);
        else
            Debug.WriteLine($"[NativeBridge] {severity}: {message}" + (ex != null ? " — " + ex.Message : ""));
    }
}
