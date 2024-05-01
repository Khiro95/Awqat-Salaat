using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;

namespace AwqatSalaat.WinUI.Controls
{
    internal class ComboBoxItemPresenter : ContentPresenter
    {
        public static readonly DependencyProperty PopupItemTemplateProperty = DependencyProperty.Register(
            "PopupItemTemplate",
            typeof(DataTemplate),
            typeof(ComboBoxItemPresenter),
            new PropertyMetadata(null));

        public static readonly DependencyProperty SelectedItemTemplateProperty = DependencyProperty.Register(
            "SelectedItemTemplate",
            typeof(DataTemplate),
            typeof(ComboBoxItemPresenter),
            new PropertyMetadata(null));

        public DataTemplate PopupItemTemplate { get => (DataTemplate)GetValue(PopupItemTemplateProperty); set => SetValue(PopupItemTemplateProperty, value); }
        public DataTemplate SelectedItemTemplate { get => (DataTemplate)GetValue(SelectedItemTemplateProperty); set => SetValue(SelectedItemTemplateProperty, value); }

        public ComboBoxItemPresenter()
        {
            Loaded += ComboBoxItemPresenter_Loaded;
        }

        private void ComboBoxItemPresenter_Loaded(object sender, RoutedEventArgs e)
        {
            if (HasAncestor(this, typeof(ComboBoxItem), 3))
            {
                ContentTemplate = PopupItemTemplate;
            }
            else
            {
                ContentTemplate = SelectedItemTemplate;
            }
        }

        public static bool HasAncestor(DependencyObject control, Type ancestorType, int ancestorLevel)
        {
            while (ancestorLevel > 0)
            {
                var parent = VisualTreeHelper.GetParent(control);

                if (parent is null)
                {
                    return false;
                }
                else if (parent.GetType() == ancestorType)
                {
                    return true;
                }

                control = parent;
                ancestorLevel--;
            }

            return false;
        }
    }
}
