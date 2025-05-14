using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Threading.Tasks;

namespace AwqatSalaat.WinUI.Controls
{
    public class CustomizedFlyout : Flyout
    {
        private bool isFirstTime = true;
        private Control flyoutPresenter;
        private ElementTheme? requestedTheme;

        public CustomizedFlyout()
        {
            this.Opened += CustomizedFlyout_Opened;
        }

        public void DisableLightDismissTemporarily()
        {
            if (flyoutPresenter?.Parent is Popup popup)
            {
                popup.IsLightDismissEnabled = false;

                Task.Delay(100).ContinueWith((task, state) =>
                {
                    if (state is CustomizedFlyout flyout)
                    {
                        flyout.DispatcherQueue.TryEnqueue(() => (flyout.flyoutPresenter.Parent as Popup).IsLightDismissEnabled = true);
                    }
                },
                this);
            }
        }

        public void SetPresenterTheme(ElementTheme theme)
        {
            if (flyoutPresenter is not null)
            {
                flyoutPresenter.RequestedTheme = theme;
            }
            else
            {
                requestedTheme = theme;
            }
        }

        protected override Control CreatePresenter()
        {
            var presenter = base.CreatePresenter();

            var displayArea = DisplayArea.GetFromWindowId(XamlRoot.ContentIslandEnvironment.AppWindowId, DisplayAreaFallback.Primary);
            double maxPresenterHeight = displayArea.WorkArea.Height / XamlRoot.RasterizationScale - 8;
            double maxPresenterWidth = displayArea.WorkArea.Width / XamlRoot.RasterizationScale - 4;

            if (presenter.MaxHeight > maxPresenterHeight)
            {
                presenter.MaxHeight = maxPresenterHeight;
            }

            if (presenter.MaxWidth > maxPresenterWidth)
            {
                presenter.MaxWidth = maxPresenterWidth;
            }

            if (requestedTheme.HasValue)
            {
                presenter.RequestedTheme = requestedTheme.Value;
                requestedTheme = null;
            }

            flyoutPresenter = presenter;

            return presenter;
        }

        private void CustomizedFlyout_Opened(object sender, object e)
        {
            var popup = flyoutPresenter.Parent as Popup;

            if (isFirstTime)
            {
                popup.GotFocus += (_, _) => flyoutPresenter?.Focus(FocusState.Programmatic);
                isFirstTime = false;
            }
        }
    }
}
