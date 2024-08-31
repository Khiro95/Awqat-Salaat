using AwqatSalaat.Helpers;
using AwqatSalaat.Interop;
using AwqatSalaat.WinUI.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Hosting;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics;

namespace AwqatSalaat.WinUI
{
    internal class TaskBarWidget : IDisposable
    {
        private const string WidgetClassName = "AwqatSalaatWidgetWinRT";
        private const string TaskBarClassName = "Shell_TrayWnd";
        private const string ReBarWindow32ClassName = "ReBarWindow32";
        private const string NotificationAreaClassName = "TrayNotifyWnd";
        private const string WidgetsButtonAutomationId = "WidgetsButton";
        private const int DefaultWidgetHostWidth = 126; // 118 for the button (2 for borders) + 4 for left margin + 4 for right margin
        private const int CompactWidgetHostWidth = 70; // 62 for the button (2 for borders) + 4 for left margin + 4 for right margin

        private readonly double dpiScale;

        private readonly IntPtr hwndShell;
        private readonly IntPtr hwndTrayNotify;
        private readonly IntPtr hwndReBar;

        private RECT taskbarRect;
        private TaskbarStructureWatcher taskbarWatcher;

        private IntPtr hwnd;
        private AppWindow appWindow;
        private WidgetSummary widgetSummary;
        private DesktopWindowXamlSource host;
        private WndProc wndProc;
        private int WidgetHostWidth;
        private int currentOffsetX = int.MinValue;
        private int currentOffsetY = 0;
        private bool disposedValue;

        public IntPtr Handle => hwnd != IntPtr.Zero ? hwnd : throw new InvalidOperationException("The widget is not initialized.");

        public event EventHandler Destroying;

        public TaskBarWidget()
        {
            hwndShell = User32.FindWindow(TaskBarClassName, null);
            hwndTrayNotify = User32.FindWindowEx(hwndShell, IntPtr.Zero, NotificationAreaClassName , null);
            hwndReBar = User32.FindWindowEx(hwndShell, IntPtr.Zero, ReBarWindow32ClassName , null);

            var dpi = User32.GetDpiForWindow(hwndShell);
            dpiScale = dpi / 96d;
            WidgetHostWidth = (int)Math.Ceiling(dpiScale * DefaultWidgetHostWidth);
        }

        public void Initialize()
        {
            host = new DesktopWindowXamlSource();

            hwnd = CreateHostWindow(hwndShell);

            var id = Win32Interop.GetWindowIdFromWindow(hwnd);

            appWindow = AppWindow.GetFromWindowId(id);
            appWindow.IsShownInSwitchers = false;
            appWindow.Destroying += AppWindow_Destroying;

            taskbarRect = SystemInfos.GetTaskBarBounds();
            appWindow.ResizeClient(new SizeInt32(WidgetHostWidth, taskbarRect.bottom - taskbarRect.top));

            host.Initialize(id);
            host.SiteBridge.ResizePolicy = Microsoft.UI.Content.ContentSizePolicy.ResizeContentToParentWindow;
            widgetSummary = new WidgetSummary() { Margin = new Microsoft.UI.Xaml.Thickness(4, 0, 4, 0), MaxHeight = 40 };
            widgetSummary.DisplayModeChanged += WidgetSummary_DisplayModeChanged;
            host.Content = widgetSummary;

            InjectIntoTaskbar();

            taskbarWatcher = new TaskbarStructureWatcher(hwndShell, () =>
            {
                System.Threading.Thread.Sleep(50);
                UpdatePositionImpl();
            });

            UpdatePositionImpl();
        }

        private void InjectIntoTaskbar()
        {
            int attempts = 0;

            while (attempts++ <= 3)
            {
                var result = User32.SetParent(hwnd, hwndShell);

                if (result != IntPtr.Zero)
                {
                    return;
                }

                System.Threading.Thread.Sleep(1000);
            }

            throw new WidgetNotInjectedException("Could not inject the widget into the taskbar.\nThe taskbar may be in use.");
        }

        private void WidgetSummary_DisplayModeChanged(DisplayMode displayMode)
        {
            int width = DefaultWidgetHostWidth;

            if (displayMode is DisplayMode.Compact or DisplayMode.CompactNoCountdown)
            {
                width = CompactWidgetHostWidth;
            }
            
            width = (int)Math.Ceiling(dpiScale * width);

            if (width != WidgetHostWidth)
            {
                WidgetHostWidth = width;

                appWindow.ResizeClient(new SizeInt32(WidgetHostWidth, appWindow.Size.Height));

                UpdatePosition();
            }
        }

        private void AppWindow_Destroying(AppWindow sender, object args)
        {
            appWindow.Destroying -= AppWindow_Destroying;
            Destroying?.Invoke(this, EventArgs.Empty);
        }

        public void Show() => appWindow.Show(false);

        public void Destroy() => appWindow.Destroy();

        public void UpdatePosition() => Task.Run(UpdatePositionImpl);

        private void UpdatePositionImpl()
        {
            bool isCentered = SystemInfos.IsTaskBarCentered();
            bool isWidgetsEnabled = SystemInfos.IsTaskBarWidgetsEnabled();

            int offsetX = 0;
            bool osRTL = System.Globalization.CultureInfo.InstalledUICulture.TextInfo.IsRightToLeft;

            User32.GetWindowRect(hwndTrayNotify, out RECT trayNotifyRect);

            IntPtr isAutoHidePtr = User32.GetProp(hwndShell, "IsAutoHideEnabled");
            bool autoHide = isAutoHidePtr == (IntPtr)1;

            if (autoHide)
            {
                User32.GetWindowRect(hwndShell, out var newRect);
                bool isHidden = newRect.top > taskbarRect.top;

                if (isHidden)
                {
                    return;
                }
            }

            var widgetsButton = isWidgetsEnabled ? taskbarWatcher.GetAutomationElement(WidgetsButtonAutomationId) : null;

            if (isCentered)
            {
                if (osRTL)
                {
                    offsetX = (widgetsButton?.CurrentBoundingRectangle.left ?? taskbarRect.right) - WidgetHostWidth;
                }
                else
                {
                    offsetX = widgetsButton?.CurrentBoundingRectangle.right ?? 0;
                }
            }
            else
            {
                if (osRTL)
                {
                    if (widgetsButton is not null && (widgetsButton.CurrentBoundingRectangle.left - trayNotifyRect.right) < WidgetHostWidth)
                    {
                        offsetX = widgetsButton.CurrentBoundingRectangle.right;
                    }
                    else
                    {
                        offsetX = trayNotifyRect.right;
                    }
                }
                else
                {
                    if (widgetsButton is not null && (trayNotifyRect.left - widgetsButton.CurrentBoundingRectangle.right) < WidgetHostWidth)
                    {
                        offsetX = widgetsButton.CurrentBoundingRectangle.left - WidgetHostWidth;
                    }
                    else
                    {
                        offsetX = trayNotifyRect.left - WidgetHostWidth;
                    }
                }
            }

            try
            {
                List<IntPtr> wnds = GetOtherInjectedWindows();

                foreach (var wnd in wnds)
                {
                    User32.GetWindowRect(wnd, out var bounds);

                    if (bounds.right < offsetX || bounds.left > (offsetX + WidgetHostWidth))
                    {
                        continue;
                    }

                    if (isCentered == osRTL)
                    {
                        offsetX = bounds.left - WidgetHostWidth;
                    }
                    else
                    {
                        offsetX = bounds.right;
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                throw;
#endif
            }

            if (osRTL)
            {
                offsetX = Math.Clamp(offsetX, trayNotifyRect.right, taskbarRect.right - WidgetHostWidth);
            }
            else
            {
                offsetX = Math.Clamp(offsetX, 0, trayNotifyRect.left - WidgetHostWidth);
            }

            User32.GetWindowRect(hwndReBar, out RECT barRect);
            int offsetY = barRect.top - taskbarRect.top;

            if (currentOffsetY != offsetY)
            {
                appWindow.MoveAndResize(new RectInt32(offsetX, offsetY, WidgetHostWidth, barRect.bottom - barRect.top));
                currentOffsetX = offsetX;
                currentOffsetY = offsetY;
            }
            else if (currentOffsetX != offsetX)
            {
                appWindow.Move(new PointInt32(offsetX, offsetY));
                currentOffsetX = offsetX;
            }
        }

        private void RegisterWindowClass()
        {
            wndProc = new WndProc(WindowProc);
            var wndClass = new WNDCLASSEX();
            wndClass.cbSize = (uint)Marshal.SizeOf(wndClass);
            wndClass.hInstance = Kernel32.GetModuleHandle(null);
            wndClass.lpfnWndProc = wndProc;
            wndClass.lpszClassName = WidgetClassName;

            User32.RegisterClassEx(ref wndClass);
        }

        private IntPtr CreateHostWindow(IntPtr parent)
        {
            RegisterWindowClass();

            var hwnd = User32.CreateWindowEx(
                dwExStyle: WindowStylesExtended.WS_EX_LAYERED,
                lpClassName: WidgetClassName,
                lpWindowName: "WidgetHost",
                dwStyle: WindowStyles.WS_POPUP,
                x: 0, y: 0,
                nWidth: 0, nHeight: 0,
                hWndParent: parent,
                hMenu: IntPtr.Zero,
                hInstance: IntPtr.Zero,
                lpParam: IntPtr.Zero);

            return hwnd;
        }

        // https://stackoverflow.com/a/28055461/4644774
        private List<IntPtr> GetOtherInjectedWindows()
        {
            List<IntPtr> childHandles = new List<IntPtr>();

            GCHandle gcChildhandlesList = GCHandle.Alloc(childHandles);
            IntPtr pointerChildHandlesList = GCHandle.ToIntPtr(gcChildhandlesList);

            try
            {
                EnumWindowProc childProc = new EnumWindowProc(EnumWindow);
                User32.EnumChildWindows(hwndShell, childProc, pointerChildHandlesList);
            }
            finally
            {
                gcChildhandlesList.Free();
            }

            return childHandles;
        }

        private bool EnumWindow(IntPtr hWnd, IntPtr lParam)
        {
            if (hWnd != this.hwnd && User32.GetAncestor(hWnd, GetAncestorFlags.GA_PARENT) == hwndShell)
            {
                StringBuilder builder = new StringBuilder(256);
                User32.GetClassName(hWnd, builder, builder.MaxCapacity);

                if (!IsSystemWindow(builder.ToString()))
                {
                    GCHandle gcChildhandlesList = GCHandle.FromIntPtr(lParam);
                    List<IntPtr> childHandles = gcChildhandlesList.Target as List<IntPtr>;
                    childHandles.Add(hWnd);
                }
            }

            return true;

            static bool IsSystemWindow(string className)
            {
                return className
                    // Common
                    is "Start"
                    or "TrayDummySearchControl"
                    or "ReBarWindow32"
                    or "TrayNotifyWnd"
                    // Windows 10 only
                    or "TrayButton"
                    or "DynamicContent2"
                    // Windows 11 only
                    or "Windows.UI.Core.CoreWindow"
                    or "Windows.UI.Composition.DesktopWindowContentBridge";
            }
        }

        private IntPtr WindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam)
        {
            var msg = (WindowMessage)uMsg;

            if (msg == WindowMessage.WM_SETTINGCHANGE && lParam != IntPtr.Zero)
            {
                string area = Marshal.PtrToStringAnsi(lParam);

                if (area is "UserInteractionMode" or "ConvertibleSlateMode")
                {
                    UpdatePosition();
                }
            }

            return User32.DefWindowProc(hWnd, uMsg, wParam, lParam);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    widgetSummary.DisplayModeChanged -= WidgetSummary_DisplayModeChanged;
                    taskbarWatcher.Dispose();
                    host.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                User32.UnregisterClass(WidgetClassName, Kernel32.GetModuleHandle(null));
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~TaskBarWidget()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
