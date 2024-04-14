using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AwqatSalaat.WinUI.Controls
{
    internal class SettingsItem : ContentControl
    {
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
            "Description",
            typeof(string),
            typeof(SettingsItem),
            new PropertyMetadata(null));

        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            "Header",
            typeof(string),
            typeof(SettingsItem),
            new PropertyMetadata(null));

        public string Description { get => (string)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
        public string Header { get => (string)GetValue(HeaderProperty); set => SetValue(HeaderProperty, value); }
    }
}
