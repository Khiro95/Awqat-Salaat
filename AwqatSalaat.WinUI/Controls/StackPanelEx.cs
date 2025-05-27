using Microsoft.UI.Xaml.Controls;
using System.Linq;

namespace AwqatSalaat.WinUI.Controls
{
    public class StackPanelEx : StackPanel
    {
        public StackPanelEx()
        {
            SizeChanged += StackPanelEx_SizeChanged;
        }

        private void StackPanelEx_SizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
        {
            if (Children.Sum(c => c.DesiredSize.Width) > MaxWidth)
            {
                Orientation = Orientation.Vertical;
            }
            else
            {
                Orientation = Orientation.Horizontal;
            }
        }
    }
}
