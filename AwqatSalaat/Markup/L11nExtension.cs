using AwqatSalaat.Helpers;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace AwqatSalaat.Markup
{
    public class L11nExtension : MarkupExtension
    {
        private class LocaleKeyConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (parameter is string key)
                {
                    return LocaleManager.Default.Get(key);
                }

                return null;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        private static readonly LocaleKeyConverter localeKeyConverter = new LocaleKeyConverter();

        public string ResKey { get; }

        public L11nExtension() : base() { }

        public L11nExtension(string resKey)
        {
            ResKey = resKey;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            Binding binding = new Binding
            {
                Path = new PropertyPath(nameof(LocaleManager.Current)),
                Source = LocaleManager.Default,
                Converter = localeKeyConverter,
                ConverterParameter = ResKey
            };

            return binding.ProvideValue(serviceProvider);
        }
    }
}
