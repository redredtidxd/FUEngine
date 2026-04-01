using System;

namespace FUEngine.Runtime;

/// <summary>
/// Contrato reservado para hosts nativos (Android/iOS) con el SDK de Google Mobile Ads.
/// El editor WPF usa el simulador de anuncios (FUEngine). En exportación, <c>Data/ads_export.json</c> indica el proveedor elegido.
/// </summary>
public class GoogleMobileAdsApi : AdsApi
{
    public override void showInterstitial(object? onClosed) =>
        throw new PlatformNotSupportedException("GoogleMobileAdsApi solo está disponible en el runtime nativo con el plugin de anuncios.");

    public override void showRewarded(object? onCompleted) =>
        throw new PlatformNotSupportedException("GoogleMobileAdsApi solo está disponible en el runtime nativo con el plugin de anuncios.");

    public override void loadInterstitial(object? onLoaded) =>
        throw new PlatformNotSupportedException("GoogleMobileAdsApi solo está disponible en el runtime nativo con el plugin de anuncios.");

    public override void loadRewarded(object? onLoaded) =>
        throw new PlatformNotSupportedException("GoogleMobileAdsApi solo está disponible en el runtime nativo con el plugin de anuncios.");
}
