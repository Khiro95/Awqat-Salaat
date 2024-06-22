using AwqatSalaat.UI.Views;
using CSDeskBand;
using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace AwqatSalaat
{
    [ComVisible(true)]
    [Guid("5F3E38A1-34C1-4A48-9B53-C15241BF1C6F")]
    [CSDeskBandRegistration(Name = WidgetName, ShowDeskBand = true)]
    public class AwqatSalaatWidget : CSDeskBandWpf
    {
        private const string WidgetName = "Awqat Salaat";
        private const int DefaultWidth = 118;
        private const int CompactWidth = 60;

        private readonly WidgetSummary uiElement;

        public AwqatSalaatWidget()
        {
            var ctx = new DispatcherSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(ctx);

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;

            int minHWidth = Properties.Settings.Default.UseCompactMode || !Properties.Settings.Default.ShowCountdown
                ? CompactWidth
                : DefaultWidth;

            uiElement = new WidgetSummary
            {
                PanelPlacement = GetPlacement(TaskbarInfo.Edge),
                RemovePopupBorderAtPlacement = true,
                Width = minHWidth,
            };

            HwndSource.SizeToContent = SizeToContent.Width;

            Options.HorizontalSize = new Size(minHWidth, TaskbarInfo.Size.Height);
            Options.MinHorizontalSize = new Size(minHWidth, 0);
            Options.MinVerticalSize = new Size(CSDeskBandOptions.TaskbarVerticalWidth, 40);

            uiElement.Dispatcher.UnhandledException += (s, e) =>
            {
                MessageBox.Show(e.Exception.Message + '\n' + e.Exception.InnerException?.Message, WidgetName);
            };

            uiElement.Dispatcher.UnhandledExceptionFilter += (s, e) =>
            {
                e.RequestCatch = false;
                MessageBox.Show(e.Exception.Message + '\n' + e.Exception.InnerException?.Message, WidgetName);
            };

            TaskbarInfo.TaskbarEdgeChanged += TaskbarInfo_TaskbarEdgeChanged;
            TaskbarInfo.TaskbarOrientationChanged += TaskbarInfo_TaskbarOrientationChanged;

            uiElement.DisplayModeChanged += UiElement_DisplayModeChanged;

            uiElement.Orientation =
                TaskbarInfo.Orientation == TaskbarOrientation.Vertical
                ? System.Windows.Controls.Orientation.Vertical
                : System.Windows.Controls.Orientation.Horizontal;
        }

        protected override void DeskbandOnClosed()
        {
            HwndSource.Dispose();
        }

        private void UiElement_DisplayModeChanged(DisplayMode displayMode)
        {
            int width;

            if (TaskbarInfo.Orientation == TaskbarOrientation.Horizontal)
            {
                if (displayMode == DisplayMode.Compact || displayMode == DisplayMode.CompactNoCountdown)
                {
                    width = CompactWidth; 
                }
                else
                {
                    width = DefaultWidth;
                }

                Options.MinHorizontalSize.Width = width;
            }
            else
            {
                width = CSDeskBandOptions.TaskbarVerticalWidth;
            }
            
            uiElement.Width = width;

            const uint WM_COMMAND = 0x0111;

            // unlock the taskbar then lock it to make it resize the deskband
            IntPtr taskbarHandle = Interop.User32.GetAncestor(ParentHandle, Interop.GetAncestorFlags.GA_PARENT);
            Interop.User32.SendMessage(taskbarHandle, WM_COMMAND, 424, 0); //unlock
            Interop.User32.SendMessage(taskbarHandle, WM_COMMAND, 424, 0); //lock
        }

        private void TaskbarInfo_TaskbarEdgeChanged(object sender, TaskbarEdgeChangedEventArgs e)
        {
            uiElement.PanelPlacement = GetPlacement(e.Edge);
        }

        private void TaskbarInfo_TaskbarOrientationChanged(object sender, TaskbarOrientationChangedEventArgs e)
        {
            uiElement.Orientation =
                e.Orientation == TaskbarOrientation.Vertical
                ? System.Windows.Controls.Orientation.Vertical
                : System.Windows.Controls.Orientation.Horizontal;
        }

        private PlacementMode GetPlacement(Edge edge)
        {
            switch (edge)
            {
                case Edge.Left:
                    return PlacementMode.Right;
                case Edge.Top:
                    return PlacementMode.Bottom;
                case Edge.Right:
                    return PlacementMode.Left;
                case Edge.Bottom:
                    return PlacementMode.Top;
            }
            return PlacementMode.Bottom;
        }

        protected override UIElement UIElement => uiElement; // Return the main wpf control

        protected override IntPtr HwndSourceHook(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            const int WM_WININICHANGE = 0x001A;

            if (msg == WM_WININICHANGE)
            {
                UI.ThemeManager.SyncWithSystemTheme();
                handled = true;
                return IntPtr.Zero;
            }

            return base.HwndSourceHook(hwnd, msg, wparam, lparam, ref handled);
        }
    }
}
