namespace FUEngine.Input;

/// <summary>Estado de teclas y puntero para Play. Actualizado desde WPF (tab Juego / ventana Player).</summary>
public sealed class PlayKeyboardSnapshot
{
    public bool W, A, S, D, Left, Right, Up, Down;
    public bool Space, E, Q, F, Enter, Shift, Ctrl;
    public bool MouseLeft;
    public double MouseX, MouseY;

    public void CopyFrom(PlayKeyboardSnapshot other)
    {
        W = other.W; A = other.A; S = other.S; D = other.D;
        Left = other.Left; Right = other.Right; Up = other.Up; Down = other.Down;
        Space = other.Space; E = other.E; Q = other.Q; F = other.F;
        Enter = other.Enter; Shift = other.Shift; Ctrl = other.Ctrl;
        MouseLeft = other.MouseLeft;
        MouseX = other.MouseX; MouseY = other.MouseY;
    }

    public void Clear()
    {
        W = A = S = D = Left = Right = Up = Down = false;
        Space = E = Q = F = Enter = Shift = Ctrl = false;
        MouseLeft = false;
        MouseX = MouseY = 0;
    }
}
