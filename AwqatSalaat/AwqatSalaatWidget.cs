using AwqatSalaat.UI.Views;
using CSDeskBand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace AwqatSalaat
{
    [ComVisible(true)]
    [Guid("5F3E38A1-34C1-4A48-9B53-C15241BF1C6F")]
    [CSDeskBandRegistration(Name = WidgetName, ShowDeskBand = true)]
    public class AwqatSalaatWidget : CSDeskBandWpf
    {
        private const string WidgetName = "Awqat Salaat";

        private WidgetSummary uiElement;

        public AwqatSalaatWidget()
        {
            uiElement = new WidgetSummary
            {
                PanelPlacement = GetPlacement(TaskbarInfo.Edge),
                RemovePopupBorderAtPlacement = true
            };

            Options.MinHorizontalSize = new Size(100, 40);
            System.Windows.Threading.Dispatcher.CurrentDispatcher.UnhandledException += (s, e) =>
            {
                try
                {
                    MessageBox.Show(e.Exception.Message + '\n' + e.Exception.InnerException?.Message, WidgetName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    throw;
                }
            };

            TaskbarInfo.TaskbarEdgeChanged += TaskbarInfo_TaskbarEdgeChanged;
        }

        private void TaskbarInfo_TaskbarEdgeChanged(object sender, TaskbarEdgeChangedEventArgs e)
        {
            uiElement.PanelPlacement = GetPlacement(e.Edge);
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
                return IntPtr.Zero;
            }

            return base.HwndSourceHook(hwnd, msg, wparam, lparam, ref handled);
        }
    }
}
