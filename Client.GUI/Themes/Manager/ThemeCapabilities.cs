using System;
using System.Collections.Generic;
using System.Text;
using static Client.GUI.App;

namespace Client.GUI.Themes.Manager
{
    public class ThemeCapabilities
    {

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



        public bool EnableMouseEffects { get; init; }
        public bool EnableTextShimmer { get; init; }
        public bool EnableGradientAnimation { get; init; }

        public static ThemeCapabilities GetCapabilities()
        {
            return _capabilities[App.Settings.SelectedTheme];
        }



    }
}
