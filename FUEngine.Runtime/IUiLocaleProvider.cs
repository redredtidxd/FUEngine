namespace FUEngine.Runtime;

/// <summary>Inyectado en <see cref="UiApi"/> para cambiar idioma en runtime (localización UI).</summary>
public interface IUiLocaleProvider
{
    void SetLocale(string? code);
    string GetLocale();
}
