using AwqatSalaat.Configurations;
using AwqatSalaat.WinUI.Controls;
using AwqatSalaat.WinUI.Views;
using Microsoft.UI.Xaml.Data;
using System;

namespace AwqatSalaat.WinUI.Converters
{
    internal class PanelStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (targetType == typeof(PrayersPanel) && value is WidgetPanelStyle panelStyle)
            {
                return panelStyle == WidgetPanelStyle.Default ? new DefaultPanel() : new PanelAlt();
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
