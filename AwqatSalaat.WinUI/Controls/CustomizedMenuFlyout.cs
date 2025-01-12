using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AwqatSalaat.WinUI.Controls
{
    public partial class CustomizedMenuFlyout : MenuFlyout
    {
        private Control flyoutPresenter;
        private ElementTheme? requestedTheme;

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

            if (requestedTheme.HasValue)
            {
                presenter.RequestedTheme = requestedTheme.Value;
                requestedTheme = null;
            }

            flyoutPresenter = presenter;

            return presenter;
        }
    }
}
