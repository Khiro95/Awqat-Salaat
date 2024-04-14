using AwqatSalaat.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using System;

namespace AwqatSalaat.WinUI.Markup
{
    [MarkupExtensionReturnType(ReturnType = typeof(string))]
    public class L11nExtension : MarkupExtension
    {
        private class LocaleKeyConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, string language)
            {
                if (parameter is string key)
                {
                    return LocaleManager.Default.Get(key);
                }

                return null;
            }

            public object ConvertBack(object value, Type targetType, object parameter, string language)
            {
                throw new NotImplementedException();
            }
        }

        private static readonly LocaleKeyConverter localeKeyConverter = new LocaleKeyConverter();

        public string Key { get; set; }

        public L11nExtension() : base() { }

        protected override object ProvideValue()
        {
            Binding binding = new Binding
            {
                Path = new PropertyPath(nameof(LocaleManager.Current)),
                Source = LocaleManager.Default,
                Converter = localeKeyConverter,
                ConverterParameter = Key
            };

            return binding;
        }
    }
}
