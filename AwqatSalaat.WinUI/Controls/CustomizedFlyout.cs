using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Threading.Tasks;

namespace AwqatSalaat.WinUI.Controls
{
    public class CustomizedFlyout : Flyout
    {
        private XamlRoot xamlRoot;
        private bool xamlRootHadChanges;
        private FrameworkElement target;
        private bool hasClosed;
        private Control latestPresenter;

        public bool ClosedBecauseOfResize { get; private set; }

        public CustomizedFlyout()
        {
            this.Opened += CustomizedFlyout_Opened;
            //this.Closing += CustomizedFlyout_Closing;
            this.Closed += CustomizedFlyout_Closed;
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

            latestPresenter = presenter;

            return presenter;
        }

        private void ShowAgain()
        {
            var attachedFlyout = GetAttachedFlyout(target);

            if (attachedFlyout is null)
            {
                ShowAt(target);
            }
            else
            {
                ShowAttachedFlyout(target);
            }
        }

        private void CustomizedFlyout_Closed(object sender, object e)
        {
            hasClosed = true;

            if (xamlRootHadChanges)
            {
                Task.Delay(200).ContinueWith(t => DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, ShowAgain));
            }
            else
            {
                target = null;
            }

            ClosedBecauseOfResize = xamlRootHadChanges;

            xamlRootHadChanges = false;
        }

        // Sadly this doesn't work because Closing event is not raised when XamlRoot get changes :(
        private void CustomizedFlyout_Closing(FlyoutBase sender, FlyoutBaseClosingEventArgs args)
        {
            if (xamlRootHadChanges)
            {
                args.Cancel = true;
            }

            xamlRootHadChanges = false;
        }

        private void CustomizedFlyout_Opened(object sender, object e)
        {
            target ??= Target;

            hasClosed = false;

            if (xamlRoot != XamlRoot)
            {
                if (xamlRoot is not null)
                {
                    xamlRoot.Changed -= XamlRoot_Changed;
                }

                xamlRoot = XamlRoot;
                xamlRoot.Changed += XamlRoot_Changed;
            }

            var popup = latestPresenter.Parent as Popup;
            popup.GotFocus += (s, ee) => latestPresenter?.Focus(FocusState.Programmatic);
        }

        private void XamlRoot_Changed(XamlRoot sender, XamlRootChangedEventArgs args)
        {
            // sometimes the change happen between setting IsOpen=false and raising Closed event
            if (IsOpen || !hasClosed)
            {
                xamlRootHadChanges = true;
            }
        }
    }
}
