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
            ThemeKey theme = Helpers.SystemInfos.IsLightThemeUsed() == true ? ThemeKey.Light : ThemeKey.Dark;
            SetTheme(theme);
        }
    }
}
