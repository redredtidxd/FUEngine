using NLua;

namespace FUEngine.Runtime;

/// <summary>API Lua <c>ads.*</c> — anuncios / monetización. El host de Play puede inyectar una subclase simulada para pruebas.</summary>
[LuaVisible]
public class AdsApi
{
    /// <summary>Encola <paramref name="action"/> en el hilo donde corre Lua (p. ej. Dispatcher WPF). Si es null, se invoca inline.</summary>
    public Action<Action>? RunOnMainThread { get; set; }

    protected static void InvokeLuaCallback(object? callback, params object[] args)
    {
        if (callback is not LuaFunction fn) return;
        try
        {
            if (args.Length == 0) fn.Call();
            else fn.Call(args);
        }
        catch
        {
            /* evitar que un callback roto tumbe el runtime */
        }
    }

    protected void Post(Action action)
    {
        if (RunOnMainThread != null) RunOnMainThread(action);
        else action();
    }

    public virtual void showInterstitial(object? onClosed) { }

    /// <summary><paramref name="onCompleted"/> recibe <c>true</c> si el usuario obtuvo la recompensa.</summary>
    public virtual void showRewarded(object? onCompleted) { }

    public virtual void showBanner(string? placementId) { }

    public virtual void loadInterstitial(object? onLoaded) { }

    public virtual void loadRewarded(object? onLoaded) { }

    public virtual bool isRewardedReady() => false;

    public virtual void setTestMode(bool enabled) { }

    public virtual void setTagForChildDirectedTreatment(bool tag) { }
}
