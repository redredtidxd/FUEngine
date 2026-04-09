using System.Text.Json;
using System.Text.Json.Serialization;

namespace FUEngine.Editor;

/// <summary>
/// Shared JSON options for all editor serializers. Single place to change indentation, naming, or reference handling.
/// </summary>
public static class SerializationDefaults
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        // Claves desconocidas en JSON antiguo no rompen la carga (campos nuevos en código / viejos en disco).
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip
    };
}
