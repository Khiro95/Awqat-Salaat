using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace AwqatSalaat.WinUI.Controls
{
    internal class LocalizedStringPresenter : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value",
            typeof(string),
            typeof(LocalizedStringPresenter),
            new PropertyMetadata(null, OnParameterChanged));

        public static readonly DependencyProperty KeyFormatProperty = DependencyProperty.Register(
            "KeyFormat",
            typeof(string),
            typeof(LocalizedStringPresenter),
            new PropertyMetadata(null, OnParameterChanged));

        private static void OnParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var presenter = (LocalizedStringPresenter)d;
            presenter.UpdateBinding();
        }

        public string Value { get => (string)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
        public string KeyFormat { get => (string)GetValue(KeyFormatProperty); set => SetValue(KeyFormatProperty, value); }

        private TextBlock textBlock;

        public LocalizedStringPresenter()
        {
            DefaultStyleKey = typeof(LocalizedStringPresenter);
        }

        private void UpdateBinding()
        {
            if (textBlock is not null)
            {
                textBlock.ClearValue(TextBlock.TextProperty);
                var binding = Markup.L11nExtension.CreateBinding(string.Format(KeyFormat ?? "{0}", Value));
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
