namespace FUEngine;

/// <summary>
/// Despacha el input del canvas al <see cref="ITool"/> actual o a los callbacks legacy del editor.
/// Solo el modo Pintar usa <see cref="ITool"/>; el resto de modos siguen en <c>EditorWindow.HandleTool*</c> (convivencia hasta migrar a más <see cref="ITool"/>).
/// </summary>
public class ToolController
{
    private ITool? _currentTool;
    private bool _isDragging;
    private readonly Action<System.Windows.Point, bool, bool> _fallbackMouseDown;
    private readonly Action<System.Windows.Point>? _fallbackMouseMove;
    private readonly Action<System.Windows.Point>? _fallbackMouseUp;

    public ToolController(
        Action<System.Windows.Point, bool, bool> fallbackMouseDown,
        Action<System.Windows.Point>? fallbackMouseMove = null,
        Action<System.Windows.Point>? fallbackMouseUp = null)
    {
        _fallbackMouseDown = fallbackMouseDown ?? throw new ArgumentNullException(nameof(fallbackMouseDown));
        _fallbackMouseMove = fallbackMouseMove;
        _fallbackMouseUp = fallbackMouseUp;
    }

    public ITool? CurrentTool
    {
        get => _currentTool;
        set
        {
            if (_isDragging && _currentTool != null)
                throw new InvalidOperationException("Cannot change tool while dragging.");
            _currentTool = value;
        }
    }

    public void HandleMouseDown(System.Windows.Point canvasPos, bool ctrl, bool shift)
    {
        _isDragging = true;
        if (_currentTool != null)
            _currentTool.OnMouseDown(canvasPos, ctrl, shift);
        else
            _fallbackMouseDown(canvasPos, ctrl, shift);
    }

    public void HandleMouseMove(System.Windows.Point canvasPos)
    {
        if (!_isDragging) return;
        if (_currentTool != null)
            _currentTool.OnMouseMove(canvasPos);
        else
            _fallbackMouseMove?.Invoke(canvasPos);
    }

    public void HandleMouseUp(System.Windows.Point canvasPos)
    {
        if (!_isDragging) return;
        _isDragging = false;
        if (_currentTool != null)
            _currentTool.OnMouseUp(canvasPos);
        else
            _fallbackMouseUp?.Invoke(canvasPos);
    }
}
