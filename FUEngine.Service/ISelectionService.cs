namespace FUEngine.Service;

/// <summary>
/// Servicio de selección del editor: centraliza qué objeto, trigger, tile,
/// canvas UI o elemento del explorador está seleccionado. Publica un evento
/// al cambiar para que los paneles se actualicen.
///
/// La implementación actual (<c>SelectionManager</c>) vive en FUEngine (WPF).
/// Mover a esta capa permite que paneles no WPF y tests unitarios observen
/// la selección sin depender de la app host.
/// </summary>
public interface ISelectionService
{
    event EventHandler? SelectionChanged;
    void ClearAll();
}
