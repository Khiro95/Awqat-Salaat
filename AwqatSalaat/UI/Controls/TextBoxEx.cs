using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AwqatSalaat.UI.Controls
{
    internal class TextBoxEx : TextBox
    {
        public static readonly DependencyProperty IsDropDownOpenProperty = DependencyProperty.Register(
            "IsDropDownOpen",
            typeof(bool),
            typeof(TextBoxEx),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty DropDownContentProperty = DependencyProperty.Register(
            "DropDownContent",
            typeof(object),
            typeof(TextBoxEx));

        public static readonly DependencyProperty AllowFocusInDropDownProperty = DependencyProperty.Register(
            "AllowFocusInDropDown",
            typeof(bool),
            typeof(TextBoxEx));

        public static readonly DependencyProperty PlaceHolderTextProperty = DependencyProperty.Register(
            "PlaceHolderText",
            typeof(string),
            typeof(TextBoxEx));

        public bool IsDropDownOpen { get => (bool)GetValue(IsDropDownOpenProperty); set => SetValue(IsDropDownOpenProperty, value); }
        public object DropDownContent { get => GetValue(DropDownContentProperty); set => SetValue(DropDownContentProperty, value); }
        public bool AllowFocusInDropDown { get => (bool)GetValue(AllowFocusInDropDownProperty); set => SetValue(AllowFocusInDropDownProperty, value); }
        public string PlaceHolderText { get => (string)GetValue(PlaceHolderTextProperty); set => SetValue(PlaceHolderTextProperty, value); }

        private TextBlock placeholderTextBlock;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            placeholderTextBlock = (TextBlock)GetTemplateChild("placeholderTextBlock");
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);

            if (e.OriginalSource == this)
            {
                ToggleDropDown();
            }
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);

            if (placeholderTextBlock != null)
            {
                placeholderTextBlock.Visibility = string.IsNullOrEmpty(Text) ? Visibility.Visible : Visibility.Collapsed;
            }

            ToggleDropDown();
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (e.Key == Key.Escape)
            {
                IsDropDownOpen = false;
            }
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            if (!IsKeyboardFocusWithin || !AllowFocusInDropDown)
            {
                IsDropDownOpen = false;
            }
        }

        private void ToggleDropDown()
        {
            IsDropDownOpen = DropDownContent != null && !string.IsNullOrEmpty(Text);
        }
    }
}
