using DiscordRPC;
using DiscordRPC.Message;
using FUEngine.Core;

namespace FUEngine;

/// <summary>
/// Rich Presence de Discord (cliente local). Falla en silencio si Discord no está disponible.
/// Imagen grande: el nombre del asset en el Developer Portal (Rich Presence → Art Assets) debe coincidir
/// carácter a carácter con <see cref="LargeImageKey"/>; si no, Discord muestra un recuadro gris.
/// Punto de extensión futura (licencia Pro): sustituir <see cref="LargeImageKey"/> según el juego del usuario.
/// </summary>
public sealed class DiscordRichPresenceService
{
    public static DiscordRichPresenceService Instance { get; } = new();

    private const string ApplicationClientId = "1486054213984452759";
    /// <summary>Nombre exacto del arte en el portal (no renombrar sin actualizar el portal).</summary>
    public const string LargeImageKey = "logo_principal";
    private const string DownloadPageUrl = "https://github.com/redredtidxd/FUEngine";

    private const int MaxDiscordFieldLength = 127;

    private DiscordRpcClient? _client;
    private bool _initAttempted;

    private DiscordRichPresenceService() { }

    /// <summary>Inicializa el cliente una sola vez (idempotente).</summary>
    public void EnsureInitialized()
    {
        if (_initAttempted) return;
        _initAttempted = true;
        try
        {
            // autoEvents: true — el ctor de un solo parámetro fija AutoEvents en solo lectura; con false habría que llamar a Invoke() en bucle.
            _client = new DiscordRpcClient(ApplicationClientId, -1, null, autoEvents: true, null);
            _client.ShutdownOnly = true;
            _client.OnConnectionFailed += Discord_OnConnectionFailed;
            _client.Initialize();
        }
        catch (Exception ex)
        {
            _client = null;
            try
            {
                EditorLog.Warning(
                    $"Discord RPC: no se pudo inicializar el cliente (Application ID / entorno). {ex.Message}",
                    "Discord");
            }
            catch { /* ignore */ }
        }
    }

    private static void Discord_OnConnectionFailed(object? sender, ConnectionFailedMessage args)
    {
        try
        {
            var pipe = args.FailedPipe >= 0 ? $"pipe {args.FailedPipe}" : "pipe desconocido";
            EditorLog.Warning(
                $"Discord RPC: no hay conexión con la app de Discord ({pipe}). ¿Discord está abierto?",
                "Discord");
        }
        catch { /* ignore */ }
    }

    public void SetHub(string details, string state)
    {
        EnsureInitialized();
        SetPresence(details, state, includeDownloadButton: true);
    }

    public void SetEditorActivity(string details, string state)
    {
        EnsureInitialized();
        SetPresence(details, state, includeDownloadButton: true);
    }

    /// <summary>Ventana Play (escena actual / principal) desde el mapa.</summary>
    public void SetStandalonePlayWindow(string projectName, bool useMainScene)
    {
        EnsureInitialized();
        var mode = useMainScene ? "Escena principal" : "Escena actual";
        SetPresence(
            "Probando juego",
            $"{Clip(mode)} · {Clip(projectName)}",
            includeDownloadButton: true);
    }

    public void Shutdown()
    {
        try
        {
            if (_client != null)
                _client.OnConnectionFailed -= Discord_OnConnectionFailed;
            _client?.Dispose();
        }
        catch { /* ignore */ }
        finally
        {
            _client = null;
        }
    }

    /// <param name="details">Línea principal bajo el nombre de la app en Discord (proyecto, modo).</param>
    /// <param name="state">Segunda línea: actividad concreta (mapa, sandbox, etc.).</param>
    private void SetPresence(string details, string state, bool includeDownloadButton)
    {
        if (_client == null || !_client.IsInitialized) return;

        var rp = new RichPresence
        {
            Details = Clip(details),
            State = Clip(state),
            Timestamps = Timestamps.Now,
            Assets = new Assets
            {
                LargeImageKey = LargeImageKey,
                LargeImageText = $"FUEngine v{EngineVersion.Current}"
            }
        };

        if (includeDownloadButton)
        {
            rp.Buttons = new[]
            {
                new DiscordRPC.Button { Label = "Descargar FUEngine", Url = DownloadPageUrl }
            };
        }

        try
        {
            _client.SetPresence(rp);
        }
        catch
        {
            // Discord rechazó el payload (longitud, URL, etc.)
        }
    }

    private static string Clip(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text.Length <= MaxDiscordFieldLength
            ? text
            : text[..MaxDiscordFieldLength];
    }
}
