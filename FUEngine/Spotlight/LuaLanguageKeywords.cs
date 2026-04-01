namespace FUEngine.Spotlight;

/// <summary>
/// Palabras reservadas según el manual Lua 5.5 §3.1 (23 entradas; en 5.4 eran 22, sin <c>global</c>).
/// Orden igual que en el manual (filas). Fuente única para Spotlight y <see cref="FUEngine.LuaEditorCompletionCatalog"/>.
/// </summary>
internal static class LuaLanguageKeywords
{
    /// <summary>Cuenta oficial del manual Lua 5.5 (lexical conventions).</summary>
    public const int ReservedWordCount = 23;

    internal const string KeywordSubtitle = "Palabra clave Lua (reservada)";

    /// <summary>
    /// Lista completa. Incluye <c>global</c> (Lua 5.5+). Si el intérprete embebido es 5.4 o anterior, <c>global</c> no es reservada en runtime, pero conviene conocerla al leer código 5.5.
    /// </summary>
    public static readonly (string Word, string Detail)[] Entries =
    {
        // — manual Lua 5.5 §3.1, fila 1 —
        ("and", "Conjunción lógica (cortocircuito)."),
        ("break", "Sale del bucle for / while / repeat más interno."),
        ("do", "Inicia bloque; también «do … end» anónimo."),
        ("else", "Rama alternativa de if."),
        ("elseif", "Condición adicional encadenada a if."),
        ("end", "Cierra function / if / for / while / repeat."),
        // — fila 2 —
        ("false", "Literal booleano falso."),
        ("for", "Bucle numérico (for i = a, b) o genérico (for k, v in …)."),
        ("function", "Declara función global, local o anónima."),
        ("global", "Lua 5.5+: declaración de ámbito global (p. ej. «global x, y» o «global *»). No existe como palabra reservada en Lua 5.4."),
        ("goto", "Salto a etiqueta ::nombre:: (desde Lua 5.2)."),
        ("if", "Condicional if … then … [elseif …] [else …] end."),
        // — fila 3 —
        ("in", "Parte de «for … in» (iteradores)."),
        ("local", "Declara variables locales al bloque."),
        ("nil", "Valor que representa ausencia de dato."),
        ("not", "Negación lógica."),
        ("or", "Disyunción lógica (cortocircuito)."),
        ("repeat", "Bucle repeat … until condición."),
        // — fila 4 —
        ("return", "Devuelve valores y sale de la función (o chunk)."),
        ("then", "Sigue a la condición en if."),
        ("true", "Literal booleano verdadero."),
        ("until", "Cierra repeat; la condición se evalúa al final del cuerpo."),
        ("while", "Bucle while condición do … end."),
    };
}
