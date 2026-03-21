using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FUEngine.Core;

namespace FUEngine;

public partial class SplashScreenWindow : Window
{
    private readonly SplashScreenConfig _config;

    public SplashScreenWindow(SplashScreenConfig? config = null)
    {
        _config = config ?? new SplashScreenConfig();
        InitializeComponent();
        Opacity = _config.FadeIn ? 0 : 1;
        LoadLogo();
    }

    private void LoadLogo()
    {
        var path = _config.LogoPath;
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            try
            {
                var uri = new Uri(Path.GetFullPath(path));
                LogoImage.Source = new BitmapImage(uri);
                LogoImage.Visibility = Visibility.Visible;
                PlaceholderLogo.Visibility = Visibility.Collapsed;
            }
            catch
            {
                PlaceholderLogo.Visibility = Visibility.Visible;
            }
        }
        else
            PlaceholderLogo.Visibility = Visibility.Visible;
    }

    public void RunThenClose(Action onComplete)
    {
        void Continue()
        {
            onComplete?.Invoke();
            Close();
        }

        if (_config.FadeIn)
        {
            var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(_config.FadeInMs));
            fadeIn.Completed += (_, _) =>
            {
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(_config.DurationMs)
                };
                timer.Tick += (_, _) =>
                {
                    timer.Stop();
                    if (_config.FadeOut)
                    {
                        var fadeOut = new System.Windows.Media.Animation.DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(_config.FadeOutMs));
                        fadeOut.Completed += (_, _) => Continue();
                        BeginAnimation(OpacityProperty, fadeOut);
                    }
                    else
                        Continue();
                };
                timer.Start();
            };
            BeginAnimation(OpacityProperty, fadeIn);
        }
        else
        {
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(_config.DurationMs)
            };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                if (_config.FadeOut)
                {
                    var fadeOut = new System.Windows.Media.Animation.DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(_config.FadeOutMs));
                    fadeOut.Completed += (_, _) => Continue();
                    BeginAnimation(OpacityProperty, fadeOut);
                }
                else
                    Continue();
            };
            timer.Start();
        }
    }
}
