using System.Collections.Generic;
using System.Globalization;
using FUEngine.Core;
using NLua;

namespace FUEngine.Runtime;

/// <summary>
/// Estado de UI en runtime: visibilidad, focus, hit-test y dispatch de bindings.
/// </summary>
public sealed class UIRuntimeBackend
{
    /// <summary>Máxima profundidad del stack de estado (menús anidados) para evitar abuso.</summary>
    private const int MaxStateStackDepth = 16;

    private readonly UIRoot? _root;
    private readonly Dictionary<string, List<UiTextLinkHit>> _textLinkHits = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _visible = new(StringComparer.OrdinalIgnoreCase);
    private readonly Stack<UiRuntimeStateSnapshot> _stateStack = new();
    private Dictionary<string, object>? _bindings;
    private string? _focusCanvasId;

    /// <summary>Errores al ejecutar callbacks de bindings (canvasId, elementId, eventName, error).</summary>
    public Action<string, string, string, string>? CallbackError { get; set; }

    public UIRuntimeBackend(UIRoot? root)
    {
        _root = root;
        EnsureDefaultVisibleCanvases();
    }

    /// <summary>Borra regiones de hipervínculos de texto; llamar al inicio del frame de UI antes de volver a registrar.</summary>
    public void ClearTextLinkHits() => _textLinkHits.Clear();

    /// <summary>Registra rectángulos en coordenadas de canvas lógico (misma escala que <see cref="UILayoutEntry.CanvasRect"/>).</summary>
    public void SetTextLinkHits(string canvasId, string elementId, IReadOnlyList<UiTextLinkHit> hits)
    {
        if (string.IsNullOrWhiteSpace(canvasId) || string.IsNullOrWhiteSpace(elementId)) return;
        var k = TextLinkStorageKey(canvasId, elementId);
        _textLinkHits.Remove(k);
        if (hits == null || hits.Count == 0) return;
        _textLinkHits[k] = new List<UiTextLinkHit>(hits);
    }

    private static string TextLinkStorageKey(string canvasId, string elementId) =>
        string.Create(CultureInfo.InvariantCulture, $"{canvasId}|{elementId}");

    /// <summary>Si no hay visibilidad explícita todavía, marca visibles todos los canvas y enfoca el superior.</summary>
    public void EnsureDefaultVisibleCanvases()
    {
        if (_root == null || _root.Canvases.Count == 0) return;
        if (_visible.Count > 0) return;
        foreach (var canvas in _root.Canvases)
        {
            if (!string.IsNullOrWhiteSpace(canvas.Id))
                _visible.Add(canvas.Id);
        }
        var ordered = GetVisibleCanvasesOrdered();
        if (ordered.Count > 0)
            _focusCanvasId = ordered[^1].Id;
    }

    public void Show(string canvasId)
    {
        if (string.IsNullOrEmpty(canvasId)) return;
        _visible.Add(canvasId);
        if (string.IsNullOrEmpty(_focusCanvasId))
            _focusCanvasId = canvasId;
    }

    public void Hide(string canvasId)
    {
        if (string.IsNullOrEmpty(canvasId)) return;
        _visible.Remove(canvasId);
        if (string.Equals(_focusCanvasId, canvasId, StringComparison.OrdinalIgnoreCase))
            _focusCanvasId = null;
    }

    public void SetFocus(string canvasId)
    {
        if (string.IsNullOrEmpty(canvasId)) return;
        if (_root?.GetCanvas(canvasId) == null) return;
        if (!_visible.Contains(canvasId))
            _visible.Add(canvasId);
        _focusCanvasId = canvasId;
    }

    public bool IsVisible(string canvasId) =>
        !string.IsNullOrEmpty(canvasId) && _visible.Contains(canvasId);

    public string? GetFocus() => _focusCanvasId;

    /// <summary>Push del estado actual de visibilidad/focus (para menús anidados). No hace nada si el stack está al límite (MaxStateStackDepth).</summary>
    public void PushState()
    {
        if (_stateStack.Count >= MaxStateStackDepth) return;
        _stateStack.Push(new UiRuntimeStateSnapshot(
            new HashSet<string>(_visible, StringComparer.OrdinalIgnoreCase),
            _focusCanvasId));
    }

    /// <summary>Pop de estado previo. Retorna false si no hay estado en stack.</summary>
    public bool PopState()
    {
        if (_stateStack.Count == 0) return false;
        var snapshot = _stateStack.Pop();
        _visible.Clear();
        foreach (var id in snapshot.VisibleCanvasIds)
            _visible.Add(id);
        _focusCanvasId = snapshot.FocusCanvasId;
        return true;
    }

    public int StateStackDepth => _stateStack.Count;

    /// <summary>Canvas visibles ordenados por ZIndex (menor->mayor).</summary>
    public IReadOnlyList<UICanvas> GetVisibleCanvasesOrdered()
    {
        if (_root == null) return new List<UICanvas>();
        var list = new List<UICanvas>();
        foreach (var c in _root.Canvases)
        {
            if (_visible.Contains(c.Id))
                list.Add(c);
        }
        list.Sort((a, b) => a.ZIndex.CompareTo(b.ZIndex));
        return list;
    }

    /// <summary>Indica si un canvas debe recibir input (focus actual o top Z si no hay focus).</summary>
    public bool ShouldReceiveInput(string canvasId)
    {
        if (string.IsNullOrEmpty(canvasId) || !_visible.Contains(canvasId)) return false;
        if (!string.IsNullOrEmpty(_focusCanvasId))
            return string.Equals(_focusCanvasId, canvasId, StringComparison.OrdinalIgnoreCase);
        var ordered = GetVisibleCanvasesOrdered();
        return ordered.Count > 0 && string.Equals(ordered[^1].Id, canvasId, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Enlaza evento (click, hover, pressed, released) a un elemento.</summary>
    public void Bind(string canvasId, string elementId, string eventName, object? callback)
    {
        if (string.IsNullOrWhiteSpace(canvasId) || string.IsNullOrWhiteSpace(elementId) || string.IsNullOrWhiteSpace(eventName) || callback == null)
            return;
        _bindings ??= new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        _bindings[BuildBindingKey(canvasId, elementId, eventName)] = callback;
    }

    /// <summary>Obtiene un elemento por Canvas y ID (para binding en scripts).</summary>
    public UIElement? GetElement(string canvasId, string elementId)
    {
        var canvas = _root?.GetCanvas(canvasId);
        if (canvas == null || string.IsNullOrEmpty(elementId)) return null;
        return FindElementById(canvas.Children, elementId);
    }

    /// <summary>Devuelve layout de UI visible transformado a viewport (para render overlay en tab Juego).</summary>
    public IReadOnlyList<UILayoutEntry> GetLayoutEntries(double viewportWidth, double viewportHeight)
    {
        var entries = new List<UILayoutEntry>();
        if (viewportWidth <= 0 || viewportHeight <= 0) return entries;

        foreach (var canvas in GetVisibleCanvasesOrdered())
        {
            if (canvas.ResolutionWidth <= 0 || canvas.ResolutionHeight <= 0) continue;
            var t = ComputeCanvasTransform(canvas, viewportWidth, viewportHeight);
            var rootRect = new UIRect { X = 0, Y = 0, Width = canvas.ResolutionWidth, Height = canvas.ResolutionHeight };
            for (int i = 0; i < canvas.Children.Count; i++)
                AddLayoutEntries(entries, canvas, canvas.Children[i], rootRect, t, depth: 0);
        }
        return entries;
    }

    /// <summary>Hit-test topmost con coordenadas de viewport.</summary>
    public bool TryHitTest(double viewportX, double viewportY, double viewportWidth, double viewportHeight, out UIHitResult hit)
    {
        hit = default;
        if (_root == null || viewportWidth <= 0 || viewportHeight <= 0) return false;

        var ordered = GetVisibleCanvasesOrdered();
        for (int i = ordered.Count - 1; i >= 0; i--)
        {
            var canvas = ordered[i];
            if (!ShouldReceiveInput(canvas.Id)) continue;
            if (canvas.ResolutionWidth <= 0 || canvas.ResolutionHeight <= 0) continue;

            var t = ComputeCanvasTransform(canvas, viewportWidth, viewportHeight);
            if (t.Scale <= 0) continue;
            var canvasX = (viewportX - t.OffsetX) / t.Scale;
            var canvasY = (viewportY - t.OffsetY) / t.Scale;
            var rootRect = new UIRect { X = 0, Y = 0, Width = canvas.ResolutionWidth, Height = canvas.ResolutionHeight };
            if (!TryHitTestElements(canvas.Children, canvasX, canvasY, rootRect, out var element, out var elementRect))
                continue;

            var linkId = TryPickTextLinkId(canvas.Id, element.Id ?? "", canvasX, canvasY) ?? "";
            var viewportRect = CanvasRectToViewportRect(elementRect, t);
            hit = new UIHitResult(canvas.Id, element.Id ?? "", element, elementRect, viewportRect, canvasX, canvasY, linkId);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Procesa evento de puntero con hit-test y binding dispatch.
    /// Retorna true si la UI consume el input (BlocksInput=true del elemento hit).
    /// </summary>
    public bool DispatchPointerEvent(double viewportX, double viewportY, double viewportWidth, double viewportHeight, string eventName)
    {
        if (!TryHitTest(viewportX, viewportY, viewportWidth, viewportHeight, out var hit))
            return false;

        DispatchBinding(hit, eventName);
        return hit.Element.BlocksInput;
    }

    private void DispatchBinding(UIHitResult hit, string eventName)
    {
        if (_bindings == null || string.IsNullOrWhiteSpace(eventName)) return;

        if (!string.IsNullOrEmpty(hit.TextLinkId) &&
            (string.Equals(eventName, "click", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(eventName, "pressed", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(eventName, "released", StringComparison.OrdinalIgnoreCase)))
        {
            var linkKey = BuildBindingKey(hit.CanvasId, hit.ElementId, "link");
            if (_bindings.TryGetValue(linkKey, out var linkCb))
            {
                try
                {
                    InvokeLinkCallback(linkCb, hit, eventName);
                }
                catch (Exception ex)
                {
                    CallbackError?.Invoke(hit.CanvasId, hit.ElementId, eventName, ex.Message);
                }
                return;
            }
        }

        var key = BuildBindingKey(hit.CanvasId, hit.ElementId, eventName);
        if (!_bindings.TryGetValue(key, out var callback)) return;

        try
        {
            InvokeCallback(callback, hit, eventName);
        }
        catch (Exception ex)
        {
            CallbackError?.Invoke(hit.CanvasId, hit.ElementId, eventName, ex.Message);
        }
    }

    private static void InvokeLinkCallback(object callback, UIHitResult hit, string eventName)
    {
        if (callback is LuaFunction luaFn)
        {
            luaFn.Call(hit.CanvasId, hit.ElementId, eventName, hit.TextLinkId, hit.CanvasX, hit.CanvasY);
            return;
        }
        if (callback is Action<string, string, string, string, double, double> a6)
        {
            a6(hit.CanvasId, hit.ElementId, eventName, hit.TextLinkId, hit.CanvasX, hit.CanvasY);
            return;
        }
        InvokeCallback(callback, hit, eventName);
    }

    private string? TryPickTextLinkId(string canvasId, string elementId, double canvasX, double canvasY)
    {
        if (!_textLinkHits.TryGetValue(TextLinkStorageKey(canvasId, elementId), out var list) || list.Count == 0)
            return null;
        for (var i = list.Count - 1; i >= 0; i--)
        {
            var h = list[i];
            if (Contains(h.Rect, canvasX, canvasY))
                return h.LinkId;
        }
        return null;
    }

    private static void InvokeCallback(object callback, UIHitResult hit, string eventName)
    {
        if (callback is LuaFunction luaFn)
        {
            luaFn.Call(hit.CanvasId, hit.ElementId, eventName, hit.CanvasX, hit.CanvasY);
            return;
        }
        if (callback is Action action)
        {
            action();
            return;
        }
        if (callback is Action<string, string, string, double, double> action5)
        {
            action5(hit.CanvasId, hit.ElementId, eventName, hit.CanvasX, hit.CanvasY);
            return;
        }
        if (callback is Action<string, string, string> action3)
        {
            action3(hit.CanvasId, hit.ElementId, eventName);
            return;
        }
        if (callback is Delegate del)
        {
            var args = BuildDelegateArguments(del.Method.GetParameters().Length, hit, eventName);
            del.DynamicInvoke(args);
            return;
        }
        throw new InvalidOperationException($"Tipo de callback no soportado: {callback.GetType().FullName}");
    }

    private static object?[] BuildDelegateArguments(int arity, UIHitResult hit, string eventName)
    {
        return arity switch
        {
            <= 0 => Array.Empty<object?>(),
            1 => new object?[] { hit },
            2 => new object?[] { hit.CanvasId, hit.ElementId },
            3 => new object?[] { hit.CanvasId, hit.ElementId, eventName },
            4 => new object?[] { hit.CanvasId, hit.ElementId, hit.CanvasX, hit.CanvasY },
            _ => new object?[] { hit.CanvasId, hit.ElementId, eventName, hit.CanvasX, hit.CanvasY }
        };
    }

    private static string BuildBindingKey(string canvasId, string elementId, string eventName) =>
        string.Create(CultureInfo.InvariantCulture, $"{canvasId}|{elementId}|{eventName}");

    private static void AddLayoutEntries(List<UILayoutEntry> entries, UICanvas canvas, UIElement element, UIRect parentRect, CanvasViewportTransform t, int depth)
    {
        var canvasRect = ResolveRect(element, parentRect);
        var viewportRect = CanvasRectToViewportRect(canvasRect, t);
        entries.Add(new UILayoutEntry(canvas.Id, element, canvasRect, viewportRect, depth));
        for (int i = 0; i < element.Children.Count; i++)
            AddLayoutEntries(entries, canvas, element.Children[i], canvasRect, t, depth + 1);
    }

    private static bool TryHitTestElements(IReadOnlyList<UIElement> elements, double x, double y, UIRect parentRect, out UIElement element, out UIRect elementRect)
    {
        for (int i = elements.Count - 1; i >= 0; i--)
        {
            var current = elements[i];
            var currentRect = ResolveRect(current, parentRect);
            if (!Contains(currentRect, x, y)) continue;

            if (TryHitTestElements(current.Children, x, y, currentRect, out element, out elementRect))
                return true;

            element = current;
            elementRect = currentRect;
            return true;
        }

        element = null!;
        elementRect = default;
        return false;
    }

    private static bool Contains(UIRect rect, double x, double y) =>
        x >= rect.X && y >= rect.Y && x <= rect.X + rect.Width && y <= rect.Y + rect.Height;

    private static UIRect ResolveRect(UIElement element, UIRect parentRect)
    {
        var anchorX = parentRect.X + parentRect.Width * element.Anchors.MinX;
        var anchorY = parentRect.Y + parentRect.Height * element.Anchors.MinY;
        var anchorW = parentRect.Width * (element.Anchors.MaxX - element.Anchors.MinX);
        var anchorH = parentRect.Height * (element.Anchors.MaxY - element.Anchors.MinY);

        var width = element.Rect.Width + (Math.Abs(anchorW) > 0.0001 ? anchorW : 0);
        var height = element.Rect.Height + (Math.Abs(anchorH) > 0.0001 ? anchorH : 0);

        return new UIRect
        {
            X = anchorX + element.Rect.X,
            Y = anchorY + element.Rect.Y,
            Width = Math.Max(0, width),
            Height = Math.Max(0, height)
        };
    }

    private static UIRect CanvasRectToViewportRect(UIRect rect, CanvasViewportTransform t)
    {
        return new UIRect
        {
            X = rect.X * t.Scale + t.OffsetX,
            Y = rect.Y * t.Scale + t.OffsetY,
            Width = rect.Width * t.Scale,
            Height = rect.Height * t.Scale
        };
    }

    private static CanvasViewportTransform ComputeCanvasTransform(UICanvas canvas, double viewportWidth, double viewportHeight)
    {
        var scaleX = viewportWidth / canvas.ResolutionWidth;
        var scaleY = viewportHeight / canvas.ResolutionHeight;
        var scale = Math.Min(scaleX, scaleY);
        var width = canvas.ResolutionWidth * scale;
        var height = canvas.ResolutionHeight * scale;
        return new CanvasViewportTransform(
            scale,
            (viewportWidth - width) / 2.0,
            (viewportHeight - height) / 2.0);
    }

    private static UIElement? FindElementById(List<UIElement> elements, string id)
    {
        foreach (var e in elements)
        {
            if (string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase)) return e;
            var found = FindElementById(e.Children, id);
            if (found != null) return found;
        }
        return null;
    }

    private sealed record UiRuntimeStateSnapshot(HashSet<string> VisibleCanvasIds, string? FocusCanvasId);
    private readonly record struct CanvasViewportTransform(double Scale, double OffsetX, double OffsetY);
}

public readonly record struct UILayoutEntry(string CanvasId, UIElement Element, UIRect CanvasRect, UIRect ViewportRect, int Depth);

public readonly record struct UIHitResult(
    string CanvasId,
    string ElementId,
    UIElement Element,
    UIRect CanvasRect,
    UIRect ViewportRect,
    double CanvasX,
    double CanvasY,
    string TextLinkId = "")
{
    public bool BlocksInput => Element.BlocksInput;
}

/// <summary>Golpe de puntero sobre un fragmento <c>&lt;link&gt;</c> en texto UI (coordenadas canvas).</summary>
public readonly record struct UiTextLinkHit(string LinkId, UIRect Rect);
