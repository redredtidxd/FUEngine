using System;
using System.Text;
using System.Windows;
using System.Windows.Input;
using FUEngine.Core;

namespace FUEngine;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        LoadVersionInfo();

        KeyDown += (s, e) => { if (e.Key == Key.Escape) Close(); };
    }

    private void LoadVersionInfo()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Versión {EngineVersion.Current}");
        sb.AppendLine();
        sb.AppendLine("Motor y editor 2D tile-based");
        sb.AppendLine($"Runtime: .NET {Environment.Version}");
        sb.AppendLine($"OS: {GetOsInfo()}");

        TxtVersion.Text = sb.ToString();
    }

    private static string GetOsInfo()
    {
        try
        {
            if (Environment.OSVersion.Version.Build >= 22000)
                return "Windows 11";
            return Environment.OSVersion.ToString();
        }
        catch
        {
            return Environment.OSVersion.ToString();
        }
    }

    private void BtnCerrar_OnClick(object sender, RoutedEventArgs e) => Close();
}
