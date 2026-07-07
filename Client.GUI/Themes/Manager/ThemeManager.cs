using Client.GUI;
using Client.GUI.Application;
using Client.GUI.Themes.Manager;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using static Client.GUI.App;



public static class ThemeManager
{
    private static Window? _window;

    public static AppTheme CurrentTheme { get; private set; } = AppTheme.Default;

    public static event Action<AppTheme>? ThemeChanged;


    private static readonly Dictionary<AppTheme, string> ThemeMap = new()
    {
        { AppTheme.Default,     "ms-appx:///Themes/DefaultTheme.xaml" },
        { AppTheme.Psychedelic, "ms-appx:///Themes/PsychedelicTheme.xaml" },
        { AppTheme.Dark,        "ms-appx:///Themes/DarkTheme.xaml" },
        { AppTheme.Light,       "ms-appx:///Themes/LightTheme.xaml" }
    };


    private static readonly Dictionary<AppTheme, ThemeCapabilities> _capabilities = new()
    {
        {
            AppTheme.Default,
            new ThemeCapabilities
            {
                EnableMouseEffects = false,
                EnableTextShimmer = false,
                EnableGradientAnimation = false
            }
        },
        {
            AppTheme.Psychedelic,
            new ThemeCapabilities
            {
                EnableMouseEffects = true,
                EnableTextShimmer = true,
                EnableGradientAnimation = true
            }
        },
        {
            AppTheme.Dark,
            new ThemeCapabilities
            {
                EnableMouseEffects = false,
                EnableTextShimmer = false,
                EnableGradientAnimation = false
            }
        },
        {
            AppTheme.Light,
            new ThemeCapabilities
            {
                EnableMouseEffects = false,
                EnableTextShimmer = false,
                EnableGradientAnimation = false
            }

        }
    };


    public static void Initialize(Window window)
    {
        _window = window;
    }

    public static void Apply(AppTheme theme)
    {
        if (_window == null)
            throw new InvalidOperationException("ThemeManager not initialized");

        if (theme == CurrentTheme)
            return;

        var previous = CurrentTheme;
        CurrentTheme = theme;

        var dictionaries = Application.Current.Resources.MergedDictionaries;

        var newTheme = new ResourceDictionary
        {
            Source = new Uri(ThemeMap[theme])
        };

        // find current theme and replace
        for (int i = 0; i < dictionaries.Count; i++)
        {
            var src = dictionaries[i].Source?.OriginalString;

            if (src == ThemeMap[previous])
            {
                dictionaries[i] = newTheme;
                break;
            }
        }

        // UI refresh - Win3UI Secret Sauce - Forces UI to re-render with the new theme
        var content = _window.Content;
        _window.Content = null;
        _window.Content = content;

        ThemeChanged?.Invoke(theme);
    }

    public static void ApplyAndSave(AppTheme theme)
    {
        Apply(theme);

        App.Settings.SelectedTheme = theme;
        AppSettingsManager.Save(App.Settings);
    }

    public static ThemeCapabilities GetCapabilities()
    {
        return _capabilities[CurrentTheme];
    }


}