using AwqatSalaat.Helpers;
using AwqatSalaat.Interop;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Threading.Tasks;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AwqatSalaat.WinUI.Views
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MoreInfoWindow : Window
    {
        private static MoreInfoWindow current;

        public static void Open()
        {
            current = current ?? new MoreInfoWindow();

            current.Activate();
        }

        private bool rtlLayoutFixed;
        private long tokenOnHeaderChanged;

        public MoreInfoWindow()
        {
            this.InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(titleBar);

            bool isRtlUI = System.Globalization.CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
            titleBar.FlowDirection = isRtlUI ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

            Activated += MoreInfoWindow_Activated;
            tokenOnHeaderChanged = nav.RegisterPropertyChangedCallback(NavigationView.HeaderProperty, OnHeaderChanged);
            LocaleManager.Default.CurrentChanged += LocaleManager_CurrentChanged;

            UpdateDirection();
            SetImageSource();
#if DEBUG
            root.RequestedTheme = ElementTheme.Light;

            if (WidgetSummary.Current is not null)
            {
                WidgetSummary.Current.ActualThemeChanged += (s, e) => root.RequestedTheme = s.RequestedTheme;
            }
#endif
            Closed += MoreInfoWindow_Closed;
        }

        private void OnHeaderChanged(DependencyObject sender, DependencyProperty dp)
        {
            UpdateTitle();
        }

        private void MoreInfoWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (!rtlLayoutFixed && System.Globalization.CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft)
            {
                FixWindowRtlLayout();
                rtlLayoutFixed = true;
            }

            if (args.WindowActivationState == WindowActivationState.Deactivated)
            {
                try
                {
                    VisualStateManager.GoToState(root, "Deactivated", true);
                }
                catch (Exception ex)
                {
#if DEBUG
                    //throw;
#endif
                }
            }
            else
            {
                VisualStateManager.GoToState(root, "ActivatedState", true);
            }
        }

        private void LocaleManager_CurrentChanged(object sender, EventArgs e)
        {
            UpdateDirection();
        }

        private void UpdateDirection()
        {
            root.FlowDirection = LocaleManager.Default.CurrentCulture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            FrameNavigationOptions navOptions = new FrameNavigationOptions();
            navOptions.TransitionInfoOverride = args.RecommendedNavigationTransitionInfo;

            Type pageType = null;

            if (args.SelectedItemContainer == calendarItem)
            {
                pageType = typeof(CalendarPage);
            }
            else if (args.SelectedItemContainer == learnItem)
            {
                pageType = typeof(LearnPage);
            }

            contentFrame.NavigateToType(pageType, null, navOptions);
            UpdateTitle();
        }

        private void UpdateTitle()
        {
            Title = nav.Header + $" - {LocaleManager.Default.Get("Data.AppName")}";
            appTitle.Text = Title;
        }

        private void MoreInfoWindow_Closed(object sender, WindowEventArgs args)
        {
            if (current == this)
            {
                current = null;
            }

            LocaleManager.Default.CurrentChanged -= LocaleManager_CurrentChanged;
            nav.UnregisterPropertyChangedCallback(NavigationView.HeaderProperty, tokenOnHeaderChanged);

            if (contentFrame.Content is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        private void FixWindowRtlLayout()
        {
            const int GWL_EXSTYLE = -20;
            const uint WS_EX_LAYOUTRTL = 0x00400000;

            var hwnd = WindowNative.GetWindowHandle(this);

            var exstyle = User32.GetWindowLong(hwnd, GWL_EXSTYLE);
            exstyle |= WS_EX_LAYOUTRTL;
            User32.SetWindowLong(hwnd, GWL_EXSTYLE, exstyle);
        }

        private async Task SetImageSource()
        {
#if PACKAGED
            var resourceContext = new Windows.ApplicationModel.Resources.Core.ResourceContext();
            resourceContext.QualifierValues["targetsize"] = "20";
            var namedResource = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap[@"Files/Images/applist.png"];
            var resourceCandidate = namedResource.Resolve(resourceContext);
            var imageFileStream = await resourceCandidate.GetValueAsStreamAsync();
            var bitmapImage = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
            bitmapImage.SetSourceAsync(imageFileStream);
            icon.Source = bitmapImage;
#else
            icon.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/app_icon_20.png"));
#endif
        }
    }
}
