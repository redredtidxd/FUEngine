using System;
using System.Threading.Tasks;
using FUEngine.Runtime;

namespace FUEngine;

/// <summary>Simula cargas y visualización de anuncios en Play (sin SDK). Registra eventos en consola del editor.</summary>
public sealed class SimulatedAdsApi : AdsApi
{
    private readonly Action<string>? _log;
    private bool _testMode = true;
    private bool _interstitialReady;
    private bool _rewardedReady;
    private bool _childDirected;

    public SimulatedAdsApi(Action<Action> runOnMainThread, Action<string>? log = null)
    {
        RunOnMainThread = runOnMainThread;
        _log = log;
    }

    private void Log(string msg) => _log?.Invoke(msg);

    public override void setTestMode(bool enabled)
    {
        _testMode = enabled;
        Log($"[ads] setTestMode({enabled})");
    }

    public override void setTagForChildDirectedTreatment(bool tag)
    {
        _childDirected = tag;
        Log($"[ads] setTagForChildDirectedTreatment({tag})");
    }

    public override void loadInterstitial(object? onLoaded)
    {
        Log("[ads] loadInterstitial…");
        DelayThen(() =>
        {
            _interstitialReady = true;
            Log("[ads] interstitial listo (simulado)");
            InvokeLuaCallback(onLoaded);
        });
    }

    public override void loadRewarded(object? onLoaded)
    {
        Log("[ads] loadRewarded…");
        DelayThen(() =>
        {
            _rewardedReady = true;
            Log("[ads] rewarded listo (simulado)");
            InvokeLuaCallback(onLoaded);
        });
    }

    public override bool isRewardedReady() => _rewardedReady;

    public override void showInterstitial(object? onClosed)
    {
        if (!_interstitialReady)
        {
            Log("[ads] showInterstitial: no cargado — cerrando con error simulado");
            InvokeLuaCallback(onClosed, false);
            return;
        }
        Log("[ads] showInterstitial (simulado ~0.8s)");
        DelayThen(() =>
        {
            _interstitialReady = _testMode;
            Log("[ads] interstitial cerrado");
            InvokeLuaCallback(onClosed, true);
        }, 800);
    }

    public override void showRewarded(object? onCompleted)
    {
        if (!_rewardedReady)
        {
            Log("[ads] showRewarded: no listo");
            InvokeLuaCallback(onCompleted, false);
            return;
        }
        Log("[ads] showRewarded (simulado ~1s, usuario acepta recompensa)");
        DelayThen(() =>
        {
            _rewardedReady = _testMode;
            Log("[ads] rewarded completado");
            InvokeLuaCallback(onCompleted, true);
        }, 1000);
    }

    public override void showBanner(string? placementId)
    {
        Log($"[ads] showBanner({placementId ?? "default"})");
    }

    private void DelayThen(Action work, int ms = 500)
    {
        Task.Run(async () =>
        {
            await Task.Delay(ms).ConfigureAwait(false);
            Post(() => work());
        });
    }
}
