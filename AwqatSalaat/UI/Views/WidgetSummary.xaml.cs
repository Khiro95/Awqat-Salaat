using AwqatSalaat.Helpers;
using AwqatSalaat.Properties;
using AwqatSalaat.UI.ViewModels;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace AwqatSalaat.UI.Views
{
    /// <summary>
    /// Interaction logic for WidgetSummary.xaml
    /// </summary>
    public partial class WidgetSummary : UserControl
    {
        private WidgetViewModel ViewModel => DataContext as WidgetViewModel;

        public static DependencyProperty PanelPlacementProperty = DependencyProperty.Register(
            nameof(PanelPlacement),
            typeof(PlacementMode),
            typeof(WidgetSummary),
            new FrameworkPropertyMetadata(PlacementMode.Bottom));

        public static DependencyProperty OrientationProperty = DependencyProperty.Register(
            nameof(Orientation),
            typeof(Orientation),
            typeof(WidgetSummary),
            new FrameworkPropertyMetadata(SystemInfos.IsTaskBarHorizontal() ? Orientation.Horizontal : Orientation.Vertical));

        public PlacementMode PanelPlacement
        {
            get => (PlacementMode)GetValue(PanelPlacementProperty);
            set => SetValue(PanelPlacementProperty, value);
        }

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        public bool RemovePopupBorderAtPlacement
        {
            get => popup?.RemoveBorderAtPlacement ?? false;
            set
            {
                if (popup != null)
                {
                    popup.RemoveBorderAtPlacement = value;
                }
            }
        }

        public WidgetSummary()
        {
            InitializeComponent();
            popup.Closed += (_, __) =>
            {
                if (ViewModel.WidgetSettings.IsOpen && ViewModel.WidgetSettings.Settings.IsConfigured)
                {
                    ViewModel.WidgetSettings.Cancel.Execute(null);
                }
            };
            LocaleManager.CurrentChanged += (prev, curr) => UpdateDirection();
            UpdateDirection();
        }

        private void UpdateDirection()
        {
            this.FlowDirection = Properties.Resources.Culture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            this.Language = System.Windows.Markup.XmlLanguage.GetLanguage(Properties.Resources.Culture.IetfLanguageTag);
        }
    }
}
