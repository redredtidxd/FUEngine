namespace FUEngine.Service;

/// <summary>
/// Localizador de servicios minimalista para FUEngine. Permite registrar y obtener
/// implementaciones de interfaces de servicio sin requerir un contenedor DI completo.
///
/// Uso típico en el arranque:
/// <code>
/// ServiceLocator.Register&lt;IAudioSystem&gt;(new AudioSystem(...));
/// ServiceLocator.Register&lt;IEditorLog&gt;(new ConsoleEditorLog());
/// </code>
///
/// Consumo:
/// <code>
/// var audio = ServiceLocator.Get&lt;IAudioSystem&gt;();
/// </code>
///
/// Este patrón mantiene la compatibilidad con el enfoque actual del proyecto (sin IoC)
/// mientras permite migrar gradualmente a interfaces.
/// </summary>
public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new();

    public static void Register<T>(T implementation) where T : class
    {
        _services[typeof(T)] = implementation ?? throw new ArgumentNullException(nameof(implementation));
    }

    public static T Get<T>() where T : class
    {
        if (_services.TryGetValue(typeof(T), out var svc))
            return (T)svc;
        throw new InvalidOperationException(
            $"Servicio '{typeof(T).Name}' no registrado. Llama a ServiceLocator.Register<{typeof(T).Name}>() en el arranque.");
    }

    public static T? TryGet<T>() where T : class
    {
        return _services.TryGetValue(typeof(T), out var svc) ? (T)svc : null;
    }

    public static bool IsRegistered<T>() where T : class
    {
        return _services.ContainsKey(typeof(T));
    }

    public static void Clear()
    {
        _services.Clear();
    }
}
