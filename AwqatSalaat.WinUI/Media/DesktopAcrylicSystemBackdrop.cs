using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace AwqatSalaat.WinUI.Media
{
    internal class DesktopAcrylicSystemBackdrop : SystemBackdrop
    {
        private DesktopAcrylicController acrylicController;

        protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
        {
            // Call the base method to initialize the default configuration object.
            base.OnTargetConnected(connectedTarget, xamlRoot);

            if (acrylicController is null)
            {
                acrylicController = new DesktopAcrylicController();
                // Set configuration.
                SystemBackdropConfiguration defaultConfig = GetDefaultSystemBackdropConfiguration(connectedTarget, xamlRoot);
                defaultConfig.IsInputActive = true;
                acrylicController.SetSystemBackdropConfiguration(defaultConfig);
            }

            // Add target.
            acrylicController.AddSystemBackdropTarget(connectedTarget);
        }

        protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop disconnectedTarget)
        {
            base.OnTargetDisconnected(disconnectedTarget);

            acrylicController.RemoveSystemBackdropTarget(disconnectedTarget);
            acrylicController = null;
        }
    }
}
