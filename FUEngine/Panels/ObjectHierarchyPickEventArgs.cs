using System;
using FUEngine.Core;

namespace FUEngine;

/// <summary>Selección de objeto en la jerarquía con modificadores de teclado (Ctrl / Mayús).</summary>
public sealed class ObjectHierarchyPickEventArgs : EventArgs
{
    public ObjectHierarchyPickEventArgs(ObjectInstance instance, bool control, bool shift)
    {
        Instance = instance;
        Control = control;
        Shift = shift;
    }

    public ObjectInstance Instance { get; }
    public bool Control { get; }
    public bool Shift { get; }
}
