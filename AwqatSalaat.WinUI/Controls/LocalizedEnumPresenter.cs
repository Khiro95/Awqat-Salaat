using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace AwqatSalaat.WinUI.Controls
{
    internal class LocalizedEnumPresenter : Control
    {
        public static readonly DependencyProperty EnumValueProperty = DependencyProperty.Register(
            "EnumValue",
            typeof(string),
            typeof(LocalizedEnumPresenter),
            new PropertyMetadata(null, OnParameterChanged));

        public static readonly DependencyProperty KeyFormatProperty = DependencyProperty.Register(
            "KeyFormat",
            typeof(string),
            typeof(LocalizedEnumPresenter),
            new PropertyMetadata(null, OnParameterChanged));

        private static void OnParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var presenter = (LocalizedEnumPresenter)d;
            presenter.UpdateBinding();
        }

        public string EnumValue { get => (string)GetValue(EnumValueProperty); set => SetValue(EnumValueProperty, value); }
        public string KeyFormat { get => (string)GetValue(KeyFormatProperty); set => SetValue(KeyFormatProperty, value); }

        private TextBlock textBlock;

        public LocalizedEnumPresenter()
        {
            DefaultStyleKey = typeof(LocalizedEnumPresenter);
        }

        private void UpdateBinding()
        {
            if (textBlock is not null)
            {
                textBlock.ClearValue(TextBlock.TextProperty);
                var binding = Markup.L11nExtension.CreateBinding(string.Format(KeyFormat ?? "{0}", EnumValue));
                BindingOperations.SetBinding(textBlock, TextBlock.TextProperty, binding);
            }
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            textBlock = GetTemplateChild("textBlock") as TextBlock;

            UpdateBinding();
        }
    }
}
