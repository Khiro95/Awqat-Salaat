using AwqatSalaat.Helpers;
using Serilog;
using System;

namespace AwqatSalaat.UI
{
    public enum ThemeKey : byte
    {
        Dark = 0,
        Light = 1
    }

    public static class ThemeManager
    {
        private static ThemeKey _current = ThemeKey.Dark;

        public static ThemeKey Current
        {
            get => _current;
            private set
            {
                _current = value;
                Changed?.Invoke();
            }
        }

        public static event Action Changed;

        static ThemeManager()
        {
            SyncWithSystemTheme();
        }

        public static void SetTheme(ThemeKey theme)
        {
            Current = theme;
        }

        public static void ToggleTheme()
        {
            SetTheme(Current == ThemeKey.Dark ? ThemeKey.Light : ThemeKey.Dark);
        }

        public static void SyncWithSystemTheme()
        {
            ThemeKey theme;

            if (SystemInfos.IsAccentColorOnTaskBar() == true)
            {
                // When accent color is used, we have to figure out the theme based on the color
                var accent = SystemInfos.GetAccentColor();
                bool colorIsDark = (5 * accent.g + 2 * accent.r + accent.b) <= 8 * 200;
                theme = colorIsDark ? ThemeKey.Dark : ThemeKey.Light;
                Log.Information($"Accent color on taskbar: R={accent.r}, G={accent.g}, B={accent.b}");
            }
            else
            {
                // We use "system theme" instead of "apps theme" because the taskbar uses the former
                theme = SystemInfos.IsLightThemeUsed() == true ? ThemeKey.Light : ThemeKey.Dark;
            }

            Log.Information($"Setting theme: {theme}");

            SetTheme(theme);
        }
    }
}
