namespace FUEngine;

/// <summary>
/// Map editor tool: handles mouse down/move/up. One implementation per tool mode (paint, select, etc.).
/// </summary>
public interface ITool
{
    void OnMouseDown(System.Windows.Point canvasPos, bool ctrl, bool shift);
    void OnMouseMove(System.Windows.Point canvasPos);
    void OnMouseUp(System.Windows.Point canvasPos);
}
