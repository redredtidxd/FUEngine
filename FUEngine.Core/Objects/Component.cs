namespace FUEngine.Core;

/// <summary>Componente que se puede adjuntar a un GameObject (Sprite, Collider, Script, Light).</summary>
public abstract class Component
{
    public GameObject? Owner { get; set; }
    public virtual void Update(float deltaTime) { }
}
