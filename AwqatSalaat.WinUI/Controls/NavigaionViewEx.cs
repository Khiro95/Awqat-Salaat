using Microsoft.UI.Xaml.Controls;

namespace AwqatSalaat.WinUI.Controls
{
    internal partial class NavigaionViewEx : NavigationView
    {
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var padding = GetTemplateChild("TogglePaneTopPadding") as Grid;

            if (padding is not null)
            {
                padding.Height = 40;
            }

            var button = GetTemplateChild("TogglePaneButton") as Button;

            if (button is not null)
            {
                button.Loaded += (_, _) => button.Width = button.MinWidth + button.Padding.Left + button.Padding.Right;
            }
        }
    }
}
