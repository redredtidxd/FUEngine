// -----------------------------------------------------------------------------
// FUEngine (FUEngine.Core) — Condiciones: LICENSE.md en la raíz del repositorio.
// Copyright (c) Red Redtid. No es licencia MIT/Apache ni "open source" OSI.
// Productos comerciales: revenue share según LICENSE.md. Prohibido integrar
// publicidad de terceros en forks GitHub sin autorización escrita. Plugins
// gratuitos (no venta de extensiones). Build pública sin garantías ni servidores
// de validación del titular.
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;

namespace FUEngine.Core;

/// <summary>Objeto de juego en runtime: tiene Transform, componentes y jerarquía (parent/children). Base para entidades.</summary>
public class GameObject
{
    /// <summary>Se dispara cuando se añade un hijo a este objeto.</summary>
    public event Action<GameObject, GameObject>? ChildAdded;
    /// <summary>Se dispara cuando se quita un hijo de este objeto.</summary>
    public event Action<GameObject, GameObject>? ChildRemoved;
    /// <summary>Se dispara cuando este objeto cambia de padre.</summary>
    public event Action<GameObject, GameObject?>? ParentChanged;

    public string Name { get; set; } = "";

    /// <summary>Id de instancia del editor (<c>objetos.json</c>); usado para referencias y <c>self.id</c> en Lua.</summary>
    public string? InstanceId { get; set; }

    public Transform Transform { get; set; } = new();
    public List<Component> Components { get; set; } = new();

    /// <summary>Etiquetas (búsqueda Lua <c>world.findByTag</c>, filtros de editor). Coincidencia sin distinguir mayúsculas.</summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>Capa de dibujado (mayor = delante). Suele copiarse desde <see cref="ObjectInstance.LayerOrder"/>.</summary>
    public int RenderOrder { get; set; }

    /// <summary>En Play: marcado al final del frame tras <c>self.destroy()</c> / <c>world.destroy</c>; las consultas y updates lo ignoran antes del flush.</summary>
    public bool PendingDestroy { get; set; }

    /// <summary>Activo para scripts (<c>self.active</c>); false pausa onUpdate sin destruir.</summary>
    public bool RuntimeActive { get; set; } = true;

    /// <summary>Padre en la jerarquía (null = raíz).</summary>
    public GameObject? Parent { get; private set; }

    /// <summary>Hijos en la jerarquía (orden de render/UI).</summary>
    private readonly List<GameObject> _children = new();
    public IReadOnlyList<GameObject> Children => _children;

    public T? GetComponent<T>() where T : Component
    {
        foreach (var c in Components)
            if (c is T t) return t;
        return null;
    }

    /// <summary>Busca un componente por nombre de tipo (ej: "Health", "ScriptComponent").</summary>
    public Component? GetComponent(string typeName)
    {
        if (string.IsNullOrEmpty(typeName)) return null;
        foreach (var c in Components)
            if (string.Equals(c.GetType().Name, typeName, StringComparison.OrdinalIgnoreCase))
                return c;
        return null;
    }

    /// <summary>Establece el padre; quita de la lista de hijos del padre anterior.</summary>
    public void SetParent(GameObject? newParent)
    {
        if (newParent == this) return;
        var oldParent = Parent;
        if (oldParent == newParent) return;
        oldParent?._children.Remove(this);
        oldParent?.ChildRemoved?.Invoke(oldParent, this);
        Parent = newParent;
        newParent?._children.Add(this);
        newParent?.ChildAdded?.Invoke(newParent, this);
        ParentChanged?.Invoke(this, newParent);
    }

    /// <summary>Busca un hijo por nombre (solo primer nivel).</summary>
    public GameObject? Find(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        foreach (var c in _children)
            if (string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))
                return c;
        return null;
    }

    /// <summary>Busca recursivamente por ruta jerárquica (ej: "Enemy/Weapon").</summary>
    public GameObject? FindInHierarchy(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        var parts = path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        GameObject? current = this;
        foreach (var part in parts)
        {
            current = current?.Find(part.Trim());
            if (current == null) return null;
        }
        return current;
    }

    /// <summary>Añade un hijo al final de la lista.</summary>
    public void AddChild(GameObject child)
    {
        if (child == null || child == this) return;
        child.SetParent(this);
    }

    /// <summary>Quita este objeto de su padre (queda como raíz).</summary>
    public void RemoveFromParent()
    {
        SetParent(null);
    }

    /// <summary>Añade un componente al objeto. El Owner del componente se asigna.</summary>
    public void AddComponent(Component component)
    {
        if (component == null) return;
        component.Owner = this;
        Components.Add(component);
    }

    /// <summary>Quita un componente por tipo (por nombre, ej: "Health").</summary>
    public bool RemoveComponent(string typeName)
    {
        var c = GetComponent(typeName);
        if (c == null) return false;
        c.Owner = null;
        Components.Remove(c);
        return true;
    }
}
