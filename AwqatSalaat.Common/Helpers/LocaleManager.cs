using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace AwqatSalaat.Helpers
{
    public class LocaleManager : INotifyPropertyChanged
    {
        private static readonly List<string> AvailableLocales = new List<string>() { "ar", "en" };

        public static LocaleManager Default { get; } = new LocaleManager();


        private string _current;

        public string Current { get => _current; set => SetLocale(value); }

        public event EventHandler CurrentChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        private LocaleManager()
        {
            var lang = Properties.Settings.Default.DisplayLanguage;

            if (string.IsNullOrEmpty(lang))
            {
                lang = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            }

            SetLocale(lang);
        }

        public string Get(string key) => Properties.Resources.ResourceManager.GetString(key, Properties.Resources.Culture);

        private void SetLocale(string locale)
        {
            if (locale == _current)
            {
                return;
            }

            if (locale is null)
            {
                throw new ArgumentNullException(nameof(locale));
            }

            if (locale.Length != 2 || locale.ToLower().Any(c => c < 'a' || c > 'z'))
            {
                throw new FormatException("The locale argument must be a two-letter string.");
            }

            if (!AvailableLocales.Contains(locale))
            {
                if (_current == "en")
                {
                    return;
                }

                // Default to English
                locale = "en";
            }

            string previous = _current;
            _current = locale;
            Properties.Resources.Culture = new System.Globalization.CultureInfo(locale);
            Properties.Settings.Default.DisplayLanguage = locale;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Current)));
            CurrentChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}