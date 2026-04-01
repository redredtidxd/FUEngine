using FUEngine.Runtime;

namespace FUEngine.Input;

/// <summary><see cref="InputApi"/> leyendo <see cref="PlayKeyboardSnapshot"/> (Lua coherente con movimiento nativo).</summary>
public sealed class WpfPlayInputApi : InputApi
{
    private readonly PlayKeyboardSnapshot _snap;

    public WpfPlayInputApi(PlayKeyboardSnapshot snap) => _snap = snap;

    public override bool isKeyDown(object key)
    {
        var s = (key?.ToString() ?? "").Trim();
        if (s.Length == 0) return false;
        s = s.ToUpperInvariant();
        return s switch
        {
            "W" => _snap.W,
            "A" => _snap.A,
            "S" => _snap.S,
            "D" => _snap.D,
            "LEFT" => _snap.Left,
            "RIGHT" => _snap.Right,
            "UP" => _snap.Up,
            "DOWN" => _snap.Down,
            "SPACE" => _snap.Space,
            "E" => _snap.E,
            "Q" => _snap.Q,
            "F" => _snap.F,
            "ENTER" => _snap.Enter,
            "SHIFT" => _snap.Shift,
            "CTRL" => _snap.Ctrl,
            "ESCAPE" => false,
            _ => false
        };
    }

    public override bool isMouseDown(object button)
    {
        if (button is int i && i == 0) return _snap.MouseLeft;
        var s = (button?.ToString() ?? "").Trim().ToUpperInvariant();
        return s == "0" || s == "LEFT" ? _snap.MouseLeft : false;
    }

    public override double mouseX => _snap.MouseX;

    public override double mouseY => _snap.MouseY;
}
