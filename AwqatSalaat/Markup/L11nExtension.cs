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
                    return LocaleManager.Get(key);
                }

                return null;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public string ResKey { get; }

        public L11nExtension() : base() { }

        public L11nExtension(string resKey)
        {
            ResKey = resKey;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var propInfo = typeof(LocaleManager).GetProperty(nameof(LocaleManager.Current));

            Binding binding = new Binding
            {
                Path = new PropertyPath(propInfo),
                Converter = new LocaleKeyConverter(),
                ConverterParameter = ResKey
            };

            return binding.ProvideValue(serviceProvider);
        }
    }
}
