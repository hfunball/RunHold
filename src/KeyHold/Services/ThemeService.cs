using System.Windows.Media;
using KeyHold.Models;
using Microsoft.Win32;
using AppThemeMode = KeyHold.Models.ThemeMode;

namespace KeyHold.Services;

public static class ThemeService
{
    public static void Apply(AppThemeMode mode)
    {
        var resolved = mode == AppThemeMode.System ? ReadSystemTheme() : mode;
        SetColor("AppBackgroundBrush", resolved == AppThemeMode.Dark ? "#101318" : "#F5F7FB");
        SetColor("PanelBrush", resolved == AppThemeMode.Dark ? "#171B22" : "#FFFFFF");
        SetColor("TextBrush", resolved == AppThemeMode.Dark ? "#F5F7FA" : "#161A20");
        SetColor("MutedTextBrush", resolved == AppThemeMode.Dark ? "#A9B2C3" : "#5E6878");
        SetColor("AccentBrush", resolved == AppThemeMode.Dark ? "#35A7FF" : "#006DD9");
        SetColor("BorderBrush", resolved == AppThemeMode.Dark ? "#29313D" : "#D6DDE8");
        SetColor("DangerBrush", "#D94444");
    }

    private static AppThemeMode ReadSystemTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is int value && value == 1 ? AppThemeMode.Light : AppThemeMode.Dark;
    }

    private static void SetColor(string resourceKey, string hex)
    {
        System.Windows.Application.Current.Resources[resourceKey] =
            new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
    }
}
